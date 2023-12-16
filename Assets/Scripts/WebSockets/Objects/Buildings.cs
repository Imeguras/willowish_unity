using System;
using System.Numerics;
using System.Text.Json;
using NetTopologySuite.Geometries;

namespace willowish_unity.websockets.objects{
	public class Buildings{
		public int id {get; set;}
		public double height {get; set;}
		public Polygon? geom{get;set;}
		public Point? center{get;set;}
		public string? comments{get;set;}
		public Buildings(){
			height=0;
			geom = Polygon.Empty;
			comments="";
		}
	}
}