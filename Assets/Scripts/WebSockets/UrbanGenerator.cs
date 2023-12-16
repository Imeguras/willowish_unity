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

[XmlRoot("Root")]
public struct WsConfigFile{
	[XmlElement("host")]
	public string host;
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
		StartCoroutine(CheckLatency());
		//wait for StartCoroutine(CheckLatency()) to finish

		StartCoroutine(GetCube());


    }
	
	public IEnumerator CheckLatency(){
	 	bool dirtyMessage = false;
		using (WebSocket ws = new WebSocket(connection_string+"echo")){
			float checkmark=0; 
			//send current time and check if its returned
			ws.OnMessage += (sender, e) =>{
				string s = e.Data.ToString();
				float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out checkmark);
				dirtyMessage = true;
				
			};
			ws.OnError += (sender, e) => {
				Debug.Log("Error: "+e.Message);
			};
			ws.Connect();
			ws.Send(Time.deltaTime.ToString());			
			
			yield return new WaitUntil(() => dirtyMessage == true);
			latency = (double)((checkmark*1000)-(Time.deltaTime*1000));
			Debug.Log("Latency: "+latency);
			dirtyMessage= false; 
			
		}

	}
	public IEnumerator GetCube(){
		bool dirtyMessage = false;
		using (WebSocket ws = new WebSocket(connection_string+"cube")){
			string s="";
			ws.OnMessage += (sender, e) =>{
				s = e.Data.ToString();
				dirtyMessage = true;
			};
			ws.OnError += (sender, e) => {
				Debug.Log("Error: "+e.Message);
			};
			ws.Connect();
			yield return new WaitUntil(() => dirtyMessage == true);
			dirtyMessage= false;
			Debug.Log(s);
			StartCoroutine(parsingCoordinates(s));

		
			//string is json
			//{"$id":"1","id":0,"height":1,"geom":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0],[0,0]]]},"center":null,"comments":""}

		}
	}
	public IEnumerator parsingCoordinates(string s){
		
		List<Buildings> values = JsonSerializer.Deserialize<List<Buildings>>(s, json_options);
		foreach(Buildings b in values){
			Debug.Log(b.height);
		}

		yield return null;
	}
    void Update(){
        
    }
}
