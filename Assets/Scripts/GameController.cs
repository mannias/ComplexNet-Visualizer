/*
 * Copyright 2014 Jason Graves (GodLikeMouse/Collaboradev)
 * http://www.collaboradev.com
 *
 * This file is part of Unity - Topology.
 *
 * Unity - Topology is free software: you can redistribute it 
 * and/or modify it under the terms of the GNU General Public 
 * License as published by the Free Software Foundation, either 
 * version 3 of the License, or (at your option) any later version.
 *
 * Unity - Topology is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with Unity - Topology. If not, see http://www.gnu.org/licenses/.
 */

using UnityEngine;
using System.Collections;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		public Link linkPrefab;

		private Hashtable nodes;
		private Hashtable links;
		private GUIText statusText;
		private int nodeCount = 0;
		private int linkCount = 0;
		private GUIText nodeCountText;
		private GUIText linkCountText;
		private bool layoutLoaded = false;
		private List<CameraPositionSave> savedPositions;
		private bool textsHidden = false;
		private bool graphHidden = true;


		private string colorrx = "([0-9]+),([0-9]+),([0-9]+)" ;

		//Method for loading the GraphML layout file
		private IEnumerator LoadLayout(string sourceFile) {

			//string sourceFile = Application.dataPath + "/Data/layout.xml";
			statusText.text = "Loading file: " + sourceFile;

			//determine which platform to load for
//			string xml = null;
//			if(Application.isWebPlayer){
//				WWW www = new WWW (sourceFile);
//				yield return www;
//				xml = www.text;
//			}
//			else{
//				StreamReader sr = new StreamReader(sourceFile);
//				xml = sr.ReadToEnd();
//				sr.Close();
//			}

			XmlReader xmlReader = XmlReader.Create (new StreamReader (sourceFile));
//
//			XmlDocument xmlDoc = new XmlDocument();
//			xmlDoc.LoadXml(xml);

			statusText.text = "Loading Topology";
			int j = 0;

			while(xmlReader.Read()) {
				if (xmlReader.Name == "circle") {

					Node nodeObject = parseNode (xmlReader);
					nodes.Add (nodeObject.id, nodeObject);
					statusText.text = "Loading Topology: Node " + nodeObject.id;
					nodeCountText.text = "Nodes: " + nodeCount;
				} else if (xmlReader.Name == "line") {
					Link linkObject = parseLink (xmlReader);
					links.Add (linkObject.id, linkObject);
					statusText.text = "Loading Topology: Edge " + linkObject.id;
					linkCountText.text = "Edges: " + linkCount;
				} 

				//every 100 cycles return control to unity
				if (j++ % 100 == 0) {
					yield return true;
				}
			}
			statusText.text = "";
			layoutLoaded = true;
		}

		private Link parseLink(XmlReader xmlNode){
			string color = "";
			float x1 = 0, x2 = 0, y1 = 0, y2 = 0, stroke = 0, opacity = 0;
			if (!xmlNode.MoveToFirstAttribute ())
				return null;
			do {
				switch (xmlNode.Name) {
				case "stroke":
					color = xmlNode.Value;
					break;
				case "x1":
					x1 = float.Parse (xmlNode.Value);
					break;
				case "x2":
					x2 = float.Parse (xmlNode.Value);
					break;
				case "y1":
					y1 = float.Parse (xmlNode.Value);
					break;
				case "y2":
					y2 = float.Parse (xmlNode.Value);
					break;
				case "stroke-width":
					stroke = float.Parse (xmlNode.Value);
					break;
				case "opacity":
					opacity = float.Parse (xmlNode.Value);
					break;
				default:
					break;
				}
			} while(xmlNode.MoveToNextAttribute());

			MatchCollection matches = Regex.Matches(color, colorrx, 
			                                        RegexOptions.IgnorePatternWhitespace);
			float r = float.Parse(matches[0].Groups[1].Value)/255;
			float g = float.Parse(matches[0].Groups[2].Value)/255;
			float b = float.Parse(matches[0].Groups[3].Value)/255;
			float max = Mathf.Max(r,Mathf.Max(g,b));
			if(max > 1f){
				r /= max; g /= max; b /= max;
			}
			Color finalColor = new Color(r,g,b,opacity);

			Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
			
			linkObject.id = "Line: " + linkCount++;

			Vector3 src = new Vector3(x1,y1,0);
			Vector3 dest = new Vector3(x2,y2,0);
			linkObject.width = stroke * 0.2f;
			linkObject.source = src;
			linkObject.target = dest;
			finalColor.a = opacity;
			linkObject.color = finalColor;
			linkObject.opacity = opacity;

			linkObject.reload ();

			return linkObject;
		}

		private Node parseNode(XmlReader xmlNode){
			string color = "", name = "";
			float x1 = 0, y1 = 0, z1 = 0, radius = 0;
			if (!xmlNode.MoveToFirstAttribute ())
				return null;
			do {
				switch (xmlNode.Name) {
				case "fill":
					color = xmlNode.Value;
					break;
				case "cx":
					x1 = float.Parse (xmlNode.Value);
					break;
				case "cy":
					y1 = float.Parse (xmlNode.Value);
					break;
				case "r":
					radius = float.Parse (xmlNode.Value);
					break;
				case "name":
					name = xmlNode.Value;
					break;
				default:
					break;
				}
			} while(xmlNode.MoveToNextAttribute());
	
			MatchCollection matches = Regex.Matches (color, colorrx, 
			                                        RegexOptions.IgnorePatternWhitespace);
			float r = float.Parse (matches [0].Groups [1].Value) / 255;
			float g = float.Parse (matches [0].Groups [2].Value) / 255;
			float b = float.Parse (matches [0].Groups [3].Value) / 255;
			float max = Mathf.Max (r, Mathf.Max (g, b));
			if (max > 1f) {
				r /= max;
				g /= max;
				b /= max;
			}
			Color finalColor = new Color (r, g, b);

			Node nodeObject = Instantiate (nodePrefab, new Vector3 (x1, y1, 0), Quaternion.identity) as Node;

			nodeObject.transform.localScale = new Vector3 (radius, radius, radius);
			nodeObject.radius = radius;
			nodeObject.position = new Vector3 (x1, y1, z1);
			nodeObject.id = "Node: " + nodeCount++;
			nodeObject.name = name;
			
			nodeObject.GetComponent<Renderer> ().material.color = finalColor;

			return nodeObject;
		}

		
		
		void Update () {
			if (Input.GetKey ("i")) {
				foreach (Node node in nodes.Values) {
					node.zoomOut ();
				}
			} else if (Input.GetKey ("o")) {
				foreach (Node node in nodes.Values) {
					node.zoomIn ();
				}
			} else if (Input.GetKeyUp ("j")) {
				if (graphHidden) {
					foreach (Node node in nodes.Values) {
						node.show ();
					}
					foreach (Link link in links.Values) {
						link.show ();
					}
					graphHidden = false;
				} else {
					foreach (Node node in nodes.Values) {
						node.hide ();
					}
					foreach (Link link in links.Values) {
						link.hide ();
					}
					graphHidden = true;
				}
			} else if (Input.GetKeyUp ("n")) {
				if (layoutLoaded) {
					bool answer = EditorUtility.DisplayDialog ("Start over?", "If you press OK you will be able to load another graph and start over", "Ok", "Cancel");
					if (answer) {
						SceneManager.LoadScene ("Topology");
					}
				} else {
					string path = EditorUtility.OpenFilePanel ("Choose graph to load", "", "xml");
					if (path != "" && System.IO.File.Exists (path)) {
						StartCoroutine (LoadLayout (path));
					}
				}
			} else if (Input.GetKeyUp ("space")) {
				CameraPositionSave savedPosition = new CameraPositionSave (Camera.main.transform.position, Camera.main.transform.rotation.eulerAngles, Camera.main.transform.localScale);
				savedPositions.Add (savedPosition);			
			} else if (Input.GetKeyUp ("h")) {
				if (!textsHidden) {
					GameObject.Find ("Instructions").GetComponent<GUIText> ().enabled = false;
					GameObject.Find ("LinkCount").GetComponent<GUIText> ().enabled = false;
					GameObject.Find ("MovementSpeed").GetComponent<GUIText> ().enabled = false;
					GameObject.Find ("NodeCount").GetComponent<GUIText> ().enabled = false;
					GameObject.Find ("StatusText").GetComponent<GUIText> ().enabled = false;
					GameObject.Find ("WarpKeys").GetComponent<GUIText> ().enabled = false;
					textsHidden = true;
				} else {
					GameObject.Find ("Instructions").GetComponent<GUIText> ().enabled = true;
					GameObject.Find ("LinkCount").GetComponent<GUIText> ().enabled = true;
					GameObject.Find ("MovementSpeed").GetComponent<GUIText> ().enabled = true;
					GameObject.Find ("NodeCount").GetComponent<GUIText> ().enabled = true;
					GameObject.Find ("StatusText").GetComponent<GUIText> ().enabled = true;
					GameObject.Find ("WarpKeys").GetComponent<GUIText> ().enabled = true;
					textsHidden = false;
				}
			} else if (Input.GetKeyUp ("1")) {
				setCameraFromSavePosition (1);
			} else if (Input.GetKeyUp ("2")) {
				setCameraFromSavePosition (2);
			} else if (Input.GetKeyUp ("3")) {
				setCameraFromSavePosition (3);
			} else if (Input.GetKeyUp ("4")) {
				setCameraFromSavePosition (4);
			} else if (Input.GetKeyUp ("5")) {
				setCameraFromSavePosition (5);
			} else if (Input.GetKeyUp ("6")) {
				setCameraFromSavePosition (6);
			} else if (Input.GetKeyUp ("7")) {
				setCameraFromSavePosition (7);
			} else if (Input.GetKeyUp ("8")) {
				setCameraFromSavePosition (8);
			} else if (Input.GetKeyUp ("9")) {
				setCameraFromSavePosition (9);
			}
		}

		private void setCameraFromSavePosition(int index) {
			Camera.main.transform.position = savedPositions [index].getPosition ();
			//Camera.main.transform.rotation.eulerAngles = savedPositions[index].getScale();
			Camera.main.transform.localScale = savedPositions [index].getScale ();
		}
			
		void Start () {
			nodes = new Hashtable();
			links = new Hashtable();
			savedPositions = new List<CameraPositionSave> ();

			nodeCountText = GameObject.Find("NodeCount").GetComponent<GUIText>();
			nodeCountText.text = "Nodes: 0";
			linkCountText = GameObject.Find("LinkCount").GetComponent<GUIText>();
			linkCountText.text = "Edges: 0";
			statusText = GameObject.Find("StatusText").GetComponent<GUIText>();
			statusText.text = "Press N to load a graph";
		}
	}
}
