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
		mesh.transform.localPosition = new Vector3(0,0,1);
		//scale is 0.1
		mesh.transform.localScale = new Vector3(0.05f,0.05f,0.05f);



		meshFilter = mesh.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshRenderer = mesh.AddComponent<MeshRenderer>();

		//find material named Buildings
		meshRenderer.sharedMaterial = Resources.Load("Buildings") as Material; 
		
	}
	//TODO: this is garbage and needs to be cleaned up
	//TOOPTIMIZE: this could be done via shader which will balance the load 
	public static void UpdateMesh(Buildings building){
		Mesh mesher = new Mesh();

		
		int len_v = building.geom.Coordinates.Length-1;
		int count = len_v-1;

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
		

		List<List<Vector3>> listOfFaces = generate25Dvertices(new_vertices, building.height);
		//create new List with every vertice from listOffaces
		List<Vector3> listOfVertices = new List<Vector3>();
		foreach(var face in listOfFaces){
			foreach(var vertex in face){
				listOfVertices.Add(vertex);
			}
		}

		mesher.SetVertices(listOfVertices.ToArray());
		
		List<int> new_triangles = deconstructAndTriangulateThree(listOfFaces);
		mesher.SetTriangles(new_triangles.ToArray(),0);

		meshFilter.mesh = mesher;
		
		mesher.RecalculateNormals();
		//uvs
		List<Vector2> uvs = new List<Vector2>();
		foreach(var vertex in listOfVertices){
			uvs.Add(new Vector2(vertex.x, vertex.z));
		}
		mesher.SetUVs(0, uvs);
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

	static List<List<Vector3>> generate25Dvertices(List<Vector3> vertices, double height){
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
				face.Add(new Vector3(((Vector3)vertices[vertices.Count-1]).x, (float)height, ((Vector3)vertices[vertices.Count-1]).z));
				face.Add(new Vector3(vertex.x, (float)height, vertex.z));
				threeDimObject.Add(face);
			}
			topVertices.Add(new Vector3(vertex.x, (float)height, vertex.z));	
			oldVertex = vertex;
		}
		threeDimObject.Add(vertices);
		threeDimObject.Add(topVertices);
		

		return threeDimObject;
	}

}