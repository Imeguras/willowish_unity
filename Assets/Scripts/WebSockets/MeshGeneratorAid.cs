using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;
using WebSocketSharp;
using UnityEngine;
using System.Globalization;

using willowish_unity.websockets.objects; 


public static class MeshGeneratorAid {
	static MeshFilter meshFilter;
	static MeshRenderer meshRenderer;
	public static void setup(Vector3 pos){
		//find TerraUniversalis object
		var terra = GameObject.Find("TerraUniversalis");
		if(terra == null){
			Debug.Log("TerraUniversalis not found");
			throw new Exception("TerraUniversalis not found");
			return;
		}
		//create new child 
		var mesh = new GameObject("Mesh");
		mesh.transform.parent = terra.transform;
		mesh.transform.localPosition = pos;

		meshFilter = mesh.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshRenderer = mesh.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
		
	}
	public static void UpdateMesh(Buildings building){
		Mesh mesher = meshFilter.mesh;

		
		Vector3[] vertex = mesher.vertices;
		int[] triangles = mesher.triangles;
		Vector3[] normals = mesher.normals;
		var uvs = mesher.uv;
		
		int len_v = building.geom.Coordinates.Length-1;
		int count = len_v-1;
		
		Array.Resize(ref vertex, (vertex.Length) + (len_v));
		Debug.Log(vertex.Length);
		List<Vector3> new_vertices = new List<Vector3>();

		foreach(var coord in building.geom.Coordinates){
			if(count<0){
				break; 
			}
			//the incoming order is anticlockwise from the bottom right corner
			
			Debug.Log("Point "+ count+":"+coord.X+","+coord.Y);
			var new_vct = new Vector3((float)coord.X, (float)0, (float)coord.Y);
			new_vertices.Add(new_vct);
			vertex[(vertex.Length-1) - count] = new_vct;
			//vertex[vertex.Length - count] = new Vector3((float)coord.X, (float)building.height, (float)coord.Y);
			count--;
		}
		count = len_v;
		mesher.vertices = vertex;

		Array.Resize(ref triangles, (triangles.Length) + (len_v*2));
		//Triangulate
		
		
		List<int> new_triangles = Triangulate(new_vertices);
		triangles = new_triangles.ToArray();

		count = len_v-1;
		mesher.triangles = triangles;
		Array.Resize(ref normals, (normals.Length) + (len_v));
		foreach(var coord in building.geom.Coordinates){
			if(count<0){
				break; 
			}
			//normals[(normals.Length-1) - count*2] = Vector3.up;
			normals[(normals.Length-1) - count] = Vector3.up;
			count--;
		}
		count = len_v-1;
		mesher.normals = normals;

		Array.Resize(ref uvs, uvs.Length + (len_v));
		foreach(var coord in building.geom.Coordinates){
			if(count<0){
				break; 
			}
			uvs[(uvs.Length-1) - count] = new Vector2((float)coord.X,(float)coord.Y);
			count--;
		}
		count = len_v;
		mesher.uv = uvs;

		meshFilter.mesh = mesher;

		//dd building.geom.Coordinates to vertex
	}
	static List<int> Triangulate(List<Vector3> vertices){
        List<int> triangles = new List<int>();
        int vertexCount = vertices.Count;

        if (vertexCount < 3)
        {
            Debug.LogError("Cannot triangulate a polygon with less than 3 vertices.");
            return triangles;
        }

        List<int> vertexIndices = new List<int>();
        for (int i = 0; i < vertexCount; i++)
        {
            vertexIndices.Add(i);
        }

        int earTipIndex;
        while (vertexCount > 3)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                int prevIndex = (i - 1 + vertexCount) % vertexCount;
                int nextIndex = (i + 1) % vertexCount;

                bool isEar = IsEar(vertexIndices, i, prevIndex, nextIndex, vertices);

                if (isEar)
                {
                    earTipIndex = i;

                    triangles.Add(vertexIndices[prevIndex]);
                    triangles.Add(vertexIndices[earTipIndex]);
                    triangles.Add(vertexIndices[nextIndex]);

                    vertexIndices.RemoveAt(earTipIndex);
                    vertexCount--;

                    break;
                }
            }
        }

        triangles.Add(vertexIndices[0]);
        triangles.Add(vertexIndices[1]);
        triangles.Add(vertexIndices[2]);

        return triangles;
    }
	static bool IsEar(List<int> vertexIndices, int earTipIndex, int prevIndex, int nextIndex, List<Vector3> vertices){
        Vector3 earTip = vertices[vertexIndices[earTipIndex]];
        Vector3 prevVertex = vertices[vertexIndices[prevIndex]];
        Vector3 nextVertex = vertices[vertexIndices[nextIndex]];
        for (int i = 0; i < vertexIndices.Count; i++){
            if (i != earTipIndex && i != prevIndex && i != nextIndex){
                Vector3 testPoint = vertices[vertexIndices[i]];
                if (IsPointInsideTriangle(earTip, prevVertex, nextVertex, testPoint)){
                    return false;
                }
            }
        }

        return true;
    }

    static bool IsPointInsideTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P){
        float denominator = (B.y - C.y) * (A.x - C.x) + (C.x - B.x) * (A.y - C.y);
        float alpha = ((B.y - C.y) * (P.x - C.x) + (C.x - B.x) * (P.y - C.y)) / denominator;
        float beta = ((C.y - A.y) * (P.x - C.x) + (A.x - C.x) * (P.y - C.y)) / denominator;
        float gamma = 1 - alpha - beta;

        return alpha >= 0 && beta >= 0 && gamma >= 0;
    }

}