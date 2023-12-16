using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System;
using WebSocketSharp;
using UnityEngine;


[XmlRoot("ws_config")]
public struct WsConfigFile{
	[XmlElement("host")]
	public string host;
}


public class UrbanGenerator : MonoBehaviour{
    WebSocket ws;
	public string ws_config = "Assets/Scripts/WebSockets/ws_config.xml";
	private string connection_string = "";
	//TODO: test this in binbows
	void OnEnable(){
		try{
				//fetch ws_config from xml from ws_config
			XmlSerializer xml = new XmlSerializer(typeof(WsConfigFile));
			//check if file exists
			if(File.Exists(ws_config)){
				if(ws_config ==  "Assets/Scripts/WebSockets/ws_config.xml"){
					Debug.Log("WebSocket Config is missing, creating default");
					WsConfigFile k = new WsConfigFile();
					k.host = "ws://localhost:8080";
					using(FileStream stream = new FileStream(ws_config, FileMode.Create)) {
						xml.Serialize(stream, k);
					}

					return;
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
		ws = new WebSocket("ws://localhost:8080");
    }

    void Update(){
        
    }
}
