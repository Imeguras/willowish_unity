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
	
	
	private const string ws_boot_config = "Assets/Scripts/WebSockets/ws_config.xml";
	public string ws_config = ws_boot_config;
	private const string ws_config_EXAMPLE = "Assets/Scripts/WebSockets/ws_config_EXAMPLE.xml";
	private string connection_string = "";
	//TODO: test this in binbows
	void OnEnable(){
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
		ws = new WebSocket(ws_config);
    }

    void Update(){
        
    }
}
