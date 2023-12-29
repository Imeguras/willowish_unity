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
//using WebSocketSharp;
using UnityEngine;
using System.Globalization;
using System.Net.WebSockets; 
using willowish_unity.websockets.objects; 
using System.Threading.Tasks;
[XmlRoot("Root")]
public struct WsConfigFile{
	[XmlElement("host")]
	public string host;
}

public class TranslateRequest{
	public double lat{get; set;}
	public double lon{get; set;}
	public TranslateRequest(double lat, double lon){
		this.lat= lat; 
		this.lon= lon;
	}
}
public class MeshRequest{
	public double lat;
	public double lon;
	public double range;
}
    //TODO Might have to be rewritten entirely 
public class UrbanGenerator : MonoBehaviour{

	private JsonSerializerOptions json_options = new JsonSerializerOptions{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
		ReferenceHandler = ReferenceHandler.Preserve
	};
	
	public double latency = 0.0;

	
	private const string ws_boot_config = "Assets/Scripts/WebSockets/ws_config.xml";
	public string ws_config = ws_boot_config;
	private const string ws_config_EXAMPLE = "Assets/Scripts/WebSockets/ws_config_EXAMPLE.xml";
	[SerializeField]
	private string connection_string = "";
	//TODO: test this in binbows
	void OnEnable(){
		json_options.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
		try{
				//fetch ws_config from xml from ws_config
			XmlSerializer xml = new XmlSerializer(typeof(WsConfigFile));
			//check if file exists
			if(!File.Exists(ws_config)){
				if(ws_config.Equals(ws_boot_config)){
					Debug.Log("WebSocket Config is missing, creating default");
					//clone ws_config_EXAMPLE.xml
					if(File.Exists(ws_config_EXAMPLE)){
						File.Copy(ws_config_EXAMPLE, ws_config);
						return;
					}
				}
				Debug.Log("WebSocket Config is missing");			
				return;
			}
			using(FileStream stream = new FileStream(ws_config, FileMode.Open)) {
				var k = xml.Deserialize(stream) as WsConfigFile?;
				if (k is null){
					Debug.Log("WebSocket Config is missing");
					return;
				}
				WsConfigFile realConfig = (WsConfigFile)k;
				connection_string = realConfig.host;
			}

			
		}catch(Exception e){
			Debug.Log("Something went wrong with config fetcher");
			Debug.Log(e);
		}
		
	}
    void Start(){
		var latency = StartCoroutine(CheckLatency());
		TranslateRequest coordsMetricSRID = new TranslateRequest(0,0);
		coordsMetricSRID=getEncodedCoords(39.706731731638236, -8.762576195269904).Result; 
		
		
		 
		//Beware Longitude is X and latitude is Y in GIS software
		//SEE https://gis.stackexchange.com/questions/11626/does-y-mean-latitude-and-x-mean-longitude-in-every-gis-software
		var coords_vector = new Vector3((float)coordsMetricSRID.lon,0,(float)coordsMetricSRID.lat);
		//MeshGeneratorAid.setup(coords_vector);
		
		//{"lat":39.706731731638236, "lon":-8.762576195269904, "range": 100}
		//StartCoroutine(GetMesh(coords_vector, 10)); 
		//StartCoroutine(GetCube());


    }
	
	public IEnumerator CheckLatency(){
	 	
		
		Uri uri = new(connection_string+"echo");

		//using (WebSocket ws = new WebSocket(connection_string+"echo")){
			float checkmark=0; 
			//send current time and check if its returned
			using (ClientWebSocket ws = new ClientWebSocket()){
				var conn= ws.ConnectAsync(uri, default);
				yield return new WaitUntil(()=> conn.IsCompleted);
				ws.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(Time.deltaTime.ToString())), System.Net.WebSockets.WebSocketMessageType.Text, true, default);				
				var bytes = new byte[1024 * 4];
				var result =  ws.ReceiveAsync(bytes, default);
				yield return new WaitUntil(()=> result.IsCompleted);
				var str = System.Text.Encoding.UTF8.GetString(bytes, 0, result.Result.Count);
				float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out checkmark);
				latency = (double)((checkmark*1000)-(Time.deltaTime*1000));
				Debug.Log("Latency: "+latency);
				yield return latency; 
			}
			
		

	}
	public IEnumerator GetCube(){
		Uri uri = new(connection_string+"cube");
		using (ClientWebSocket ws = new ClientWebSocket()){
			
			var conn= ws.ConnectAsync(uri, default);
			yield return new WaitUntil(()=> conn.IsCompleted);
			var bytes = new byte[1024 * 4];
			var result = ws.ReceiveAsync(bytes, default);
			yield return new WaitUntil(()=> result.IsCompleted);

			var str = System.Text.Encoding.UTF8.GetString(bytes, 0, result.Result.Count);
			Debug.Log(str);
			StartCoroutine(dispatchMeshGeneration(str));
		}
	}

	public async Task<TranslateRequest> getEncodedCoords(double lat, double lon ){
		//send translate coords
		Uri uri = new(connection_string+"translate");
		using (ClientWebSocket ws = new ClientWebSocket()){
			await ws.ConnectAsync(uri, default);
			var send = new TranslateRequest(lat, lon);
			string json = JsonSerializer.Serialize<TranslateRequest>(send, json_options);
			await ws.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(json)), System.Net.WebSockets.WebSocketMessageType.Text, true, default);
			var bytes = new byte[1024 * 4];
			var result = await ws.ReceiveAsync(bytes, default);
			var str = System.Text.Encoding.UTF8.GetString(bytes, 0, result.Count);
			//return decodeJSONPoint(str)
			TranslateRequest ret = decodeJSONPoint(str);
			//from completed ret
			 return ret; 
		}
	}
	
	public IEnumerator GetMesh(Vector3 coords, double range){
		Uri uri = new(connection_string+"mesh");
		using (ClientWebSocket ws = new ClientWebSocket()){
			
			var conn= ws.ConnectAsync(uri, default);
			yield return new WaitUntil(()=> conn.IsCompleted);
			var send = new MeshRequest{
				lat = coords.x,
				//TODO fix this
				lon = coords.z,
				range = range
			};
			//string json = JsonSerializer.Serialize<MeshRequest>(send, json_options);
			//string json = $"{{\"lat\":{coords.x}, \"lon\":{coords.z}, \"range\": {range} }}";
			//sometimes you just need a little less gun
			string json = "{\"lat\":39.706731731638236, \"lon\":-8.762576195269904, \"range\": 10}";
			Debug.Log(json);
			ws.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(json)), System.Net.WebSockets.WebSocketMessageType.Text, true, default);
			var bytes = new byte[1024 * 4];
			
			var result = ws.ReceiveAsync(bytes, default);
			yield return new WaitUntil(()=> result.IsCompleted);
			
			var s = System.Text.Encoding.UTF8.GetString(bytes, 0, result.Result.Count);
			Debug.Log(s);
			StartCoroutine(dispatchMeshGeneration(s));

		
			//string is json
			//{"$id":"1","id":0,"height":1,"geom":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0],[0,0]]]},"center":null,"comments":""}

		}
	}
	public TranslateRequest decodeJSONPoint(string s){
		Point p = JsonSerializer.Deserialize<Point>(s, json_options);
		TranslateRequest k = new TranslateRequest(p.Coordinate.Y, p.Coordinate.X);
		return k;

	}
	public IEnumerator dispatchMeshGeneration(string s){
		
		List<Buildings> values = JsonSerializer.Deserialize<List<Buildings>>(s, json_options);
		foreach (Buildings item in values){
			MeshGeneratorAid.UpdateMesh(item);
		}
		
		yield return null;
	}
    void Update(){
        
    }
}
