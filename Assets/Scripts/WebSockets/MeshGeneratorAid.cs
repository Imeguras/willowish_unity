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
	public static Vector3 pos_spawn; 
	public static void setup(Vector3 pos){
		pos_spawn = pos;
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
		//(inverse of pos)
		terra.transform.localPosition = new Vector3(0,0,0);
		mesh.transform.localPosition = new Vector3(0,0,0);


		meshFilter = mesh.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshRenderer = mesh.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
		
	}
	//TODO: this is garbage and needs to be cleaned up
	//TOOPTIMIZE: this could be done via shader which will balance the load 
	public static void UpdateMesh(Buildings building){
		Mesh mesher = meshFilter.mesh;

		
		Vector3[] vertex = mesher.vertices;
		int[] triangles = mesher.triangles;
		Vector3[] normals = mesher.normals;
		var uvs = mesher.uv;
		
		int len_v = building.geom.Coordinates.Length-1;
		int count = len_v-1;

		int size_vertex = (vertex.Length) +(2*len_v)+(len_v*len_v);//(len_v*len_v)
		Array.Resize(ref vertex, size_vertex);
		//Array.Resize(ref vertex, (vertex.Length) + len_v);
		Debug.Log(vertex.Length);
		
		List<Vector3> new_vertices = new List<Vector3>();

		foreach(var coord in building.geom.Coordinates){
			var localX = (float)coord.X - pos_spawn.x;
			var localY = (float)coord.Y - pos_spawn.z;
			//the incoming order is anticlockwise from the bottom right corner
			var new_vct = new Vector3((float)localX, (float)0, (float)localY);
			new_vertices.Add(new_vct);
		}
		//remove last 
		new_vertices.RemoveAt(new_vertices.Count-1);
		//reverse the order
		new_vertices.Reverse();
		

		var listOfFaces = generate25Dvertices(new_vertices, building.height, ref vertex);
		Debug.Log("Faces"+listOfFaces.Count);
		Debug.Log("Vertices Buffer"+vertex.Length);
		mesher.vertices = vertex;
		//Triangulate
		List<int> new_triangles = deconstructAndTriangulateThree(listOfFaces);
		count = triangles.Length;
		//Todo wtf? theres 30 positions 
		//Debug.Log((triangles.Length)+new_triangles.Count);
		Array.Resize(ref triangles, (triangles.Length)+new_triangles.Count);
		int i = 0;
		
		foreach(var triangle in new_triangles){
			triangles[count+i] = triangle;

		
			i++;

			
		}

		mesher.triangles = triangles;
		var new_normals = genNormals(listOfFaces, mesher);
	    count = normals.Length;
		Array.Resize(ref normals, size_vertex);
		i = 0;
		foreach(var normal in new_normals){
			normals[count+i] = normal;
			i++;
		}		
		

		
		count = len_v-1;
		mesher.normals = normals;
		Array.Resize(ref uvs, size_vertex);
		
		for(int j=0; j<vertex.Length; j++){
			uvs[j] = new Vector2(vertex[j].x, vertex[j].z);
		}

		mesher.uv = uvs;

		meshFilter.mesh = mesher;
		//meshFilter.mesh.Optimize();
		//meshFilter.mesh.RecalculateBounds();
		//meshFilter.mesh.RecalculateNormals();

		//dd building.geom.Coordinates to vertex
	}
	static List<Vector3> genNormals(List<List<Vector3>> faces, Mesh _mesh = null){
		List<Vector3> normals = new List<Vector3>();
		
		for(int i=0; i<faces.Count; i++){

			var face = faces[i];
			
			const int _a =0;
			
			//this will work because every face regardless of the number of vertices it will always be contained in a 2d plane
			//var normal = Vector3.Cross(face[1]-face[0], face[2]-face[0]).normalized;
			var normal = Vector3.Cross(face[1]-face[0], face[2]-face[0]);

			for(int _j=0; _j<face.Count; _j++){
				
				//annoyingly add the same normal for each vertex in the face	
				normals.Add(normal);
			}
			
		}
		Debug.Log("Normals"+normals.Count);
		return normals;
	}
	
	static List<int> Triangulate(List<Vector3> vertices){
		

			
			List<int> triangles = new List<int>();
			//convert vertices to Ipoint list
			List<DelaunatorSharp.IPoint> points = new List<DelaunatorSharp.IPoint>();
			Vector3 normal = Vector3.Cross(vertices[1]-vertices[0], vertices[2]-vertices[0]).normalized;
			normal.x = Mathf.Abs(normal.x);
			normal.y = Mathf.Abs(normal.y);
			normal.z = Mathf.Abs(normal.z);
			foreach(var vertex in vertices){
				//align with plain, by calculating normal, normalizing it, setting it to absolute 
				
				//Now rotate all vertices to be paralel to the xz plane 
				if(normal != Vector3.up){
					var k = Quaternion.FromToRotation(normal, Vector3.up) * vertex;

					//Debug.Log("Normal"+normal);
					
					points.Add(new DelaunatorSharp.Point(k.x, k.z));

				}else{
					
					points.Add(new DelaunatorSharp.Point(vertex.x, vertex.z));
				}

		
				
			}

			DelaunatorSharp.Delaunator delaunator = new DelaunatorSharp.Delaunator(points.ToArray());
			for(int i=0; i<delaunator.Triangles.Length; i++){
				triangles.Add(delaunator.Triangles[i]);
			}
			//TODO remove triangles outside perimeter of building

			return triangles;

		

	}


	
	
	static List<int> deconstructAndTriangulateThree(List<List<Vector3>> threeDimObject){
		List<int> triangles = new List<int>();
		int offset =0;
		foreach(var face in threeDimObject){
			
			List<int> new_triangles = Triangulate(face);
			
			int a = new_triangles.Count;
			
			for(int i=0; i<a; i++){
				new_triangles[i] += offset;

				triangles.Add(new_triangles[i]);
				
			}
			
			offset += face.Count;
			
		}
		
		return triangles;

	}
	static List<List<Vector3>> generate25Dvertices(List<Vector3> vertices, double height, ref Vector3[]? buffer){
		List<List<Vector3>> threeDimObject = new List<List<Vector3>>();
		//start with base
		
		List<Vector3> topVertices = new List<Vector3>();
		Vector3? oldVertex=null;
		foreach(var vertex in vertices){
		//first add the top vertices
			if(oldVertex != null){
				List<Vector3> face = new List<Vector3>();
				face.Add((Vector3)oldVertex);
				face.Add(vertex);
				face.Add(new Vector3(vertex.x, (float)height, vertex.z));
				face.Add(new Vector3(((Vector3)oldVertex).x, (float)height, ((Vector3)oldVertex).z));
				threeDimObject.Add(face);
			}else{
				List<Vector3> face = new List<Vector3>();
				face.Add(vertex);
				face.Add(new Vector3(((Vector3)vertices[vertices.Count-1]).x, (float)0, ((Vector3)vertices[vertices.Count-1]).z));
				face.Add(new Vector3(vertex.x, (float)height, vertex.z));
				face.Add(new Vector3(((Vector3)vertices[vertices.Count-1]).x, (float)height, ((Vector3)vertices[vertices.Count-1]).z));
				threeDimObject.Add(face);
			}
			topVertices.Add(new Vector3(vertex.x, (float)height, vertex.z));	
			oldVertex = vertex;
		}
		threeDimObject.Add(vertices);

		threeDimObject.Add(topVertices);
		// Already added threeDimObject.Add(baseVertices);
		if (buffer != null){
			//shameless grab meshFilter.mesh.vertices.Length
			//TODO THIS SUCKS
			var offset = 0;
			foreach(var face in threeDimObject){
				foreach(var vertex in face){
					buffer[offset++] = vertex;

				}
			}

		}

		return threeDimObject;
	}
	
    /*static bool IsPointInsideTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P){
        float denominator = (B.y - C.y) * (A.x - C.x) + (C.x - B.x) * (A.y - C.y);
        float alpha = ((B.y - C.y) * (P.x - C.x) + (C.x - B.x) * (P.y - C.y)) / denominator;
        float beta = ((C.y - A.y) * (P.x - C.x) + (A.x - C.x) * (P.y - C.y)) / denominator;
        float gamma = 1 - alpha - beta;

        return alpha >= 0 && beta >= 0 && gamma >= 0;
    }*/
	/**
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
		triangles.Add(vertexIndices[0]);
        triangles.Add(vertexIndices[1]);
		triangles.Add(vertexIndices[2]);
       
        while (vertexCount > 3){
		
            for (int i = 0; i < vertexCount; i++){
                
				int prevIndex = (i - 1 + vertexCount) % vertexCount;
				
				int nextIndex = (i + 1) % vertexCount;

                bool isEar = IsEar(vertexIndices, i, prevIndex, nextIndex, vertices);

                if (isEar){
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
	**/
	

}