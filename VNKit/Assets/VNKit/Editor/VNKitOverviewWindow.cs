﻿using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VNKit
{

    /// <summary>
    /// This is the EditorWindow that gives an overview of all the current VNKitScenes in the  game
    /// It should allow the user to puruse the scene heirarchy as well as edit said heirarchy and the 
    /// individual scenes themselves.
    /// It should also allow manipulation of loading screens, GUI skins and other game-wide assets/variables
    /// </summary>
    public class VNKitOverviewWindow : EditorWindow
    {
        //Some metrics about the overview window 
        float nodeWindowWidth = 200.0f;
        float nodeWindowHeight = 150.0f;

        Rect SceneLayoutRect = new Rect(0, 0, 100,100);

        //Variables for zooming
        float zoomScale = 1.0f;
        Vector2 zoomOrigin = Vector2.zero;
		Vector2 zoomDelta = Vector2.zero;

        //Variables for window focus
        int focusedWindowID = 0;

        //Other variables
        GameObject startScene;
        List<Scene> scenes = new List<Scene>();
        List<Rect> nodeWindows = new List<Rect>();

        /// <summary>
        /// Simply show the Overview Window
        /// </summary>
        public static void ShowWindow()
        {
            VNKitOverviewWindow window = (VNKitOverviewWindow)EditorWindow.GetWindow(typeof(VNKitOverviewWindow));
            window.PopulateNodes();
        }

        /// <summary>
        /// When the window is first generated, populate the list of scenes
        /// </summary>
        void PopulateNodes()
        {
            Scene[] vnscenes = GameObject.FindObjectsOfType<Scene>();

            foreach (Scene scene in vnscenes)
            {
                if (scene != null)
                {
                    if (scene.StartScene)
                    {
                        startScene = scene.gameObject;
                    }

                    Rect nodeWindow = new Rect(scene.overviewX, scene.overviewY, nodeWindowWidth, nodeWindowHeight);

                    scenes.Add(scene);
                    nodeWindows.Add(nodeWindow);
                }
            }
        }

        /// <summary>
        /// Called from OnGUI
        /// Used to handle special key/mouse events such as scrolling
        /// </summary>
        void HandleEvents() 
        {
            Vector2 mousePos = Event.current.mousePosition;

            if (SceneLayoutRect.Contains(mousePos) && Event.current.type == EventType.ScrollWheel)
            {
                Vector2 delta = Event.current.delta;
                Vector2 zoomedMousePos = (mousePos - SceneLayoutRect.min) / zoomScale + zoomOrigin;

                float oldZoomScale = zoomScale;

                float zoomDelta = -delta.y / 150.0f;
                zoomScale += zoomDelta;
                zoomScale = Mathf.Clamp(zoomScale, 0.5f, 2.0f);

                zoomOrigin += (zoomedMousePos - zoomOrigin) - (oldZoomScale / zoomScale) * (zoomedMousePos - zoomOrigin);

                Event.current.Use();
            }

            if(Event.current.type == EventType.MouseDrag && Event.current.button == 2)
            {
                zoomDelta = Event.current.delta;
                zoomDelta /= zoomScale;
                zoomOrigin += zoomDelta;

                Event.current.Use();
            }
			else
			{
				zoomDelta = Vector2.zero;
			}
        }

        /// <summary>
        /// Called from OnGUI
        /// For drawing the scene layout and utility windows
        /// </summary>
        void DrawSceneLayout() 
        {
			GUILayout.Space(6);

            //Draw zoom slider 
            zoomScale = EditorGUILayout.Slider("Zoom", zoomScale, 0.5f, 2.0f);

            GUILayout.BeginVertical();
            {
                GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(SceneLayoutRect.height));

                GUILayout.FlexibleSpace();

                EditorZoomArea.Begin(zoomScale, SceneLayoutRect);

				BeginWindows();

                //Draw node windows
                for (int i = 0; i < nodeWindows.Count; i++)
                {
                    Rect window = nodeWindows[i];
                    
                    Scene scene = scenes[i];

                    if (scene != null)
                    {
                        Rect movedWindowRect = GUILayout.Window(i + 1, window, DrawNodeWindow, scene.Title);

                        nodeWindows[i] = new Rect(movedWindowRect.x + zoomDelta.x, movedWindowRect.y + zoomDelta.y, 
						                          movedWindowRect.width, movedWindowRect.height);

                        scene.overviewX = movedWindowRect.x;
                        scene.overviewY = movedWindowRect.y;
                    }
                    else
                    {
                        scenes.RemoveAt(i);
                        nodeWindows.RemoveAt(i);
                    }
                    
                }

                //Draw window doodads below
                for (int i = 0; i < nodeWindows.Count; i++)
                {
                    Rect window = nodeWindows[i];
                    Scene scene = scenes[i].GetComponent<Scene>();

                    //Draw curve between this window and a connected window
                    GameObject connectedSceneObject = scene.NextSceneObject;
					if(connectedSceneObject != null)
					{
	                    for (int j = 0; j < scenes.Count; j++)
	                    {
	                        if (scenes[j].gameObject == connectedSceneObject)
							{
	                            Rect connectedWindow = nodeWindows[j];
	                            DrawNodeCurve(window, connectedWindow);
	                            break;
	                        }
	                    }
					}
                }

                EndWindows();

				//Draw window doodads on top
				for (int i = 0; i < nodeWindows.Count; i++)
				{
					Rect window = nodeWindows[i];
					Scene scene = scenes[i].GetComponent<Scene>();
					
					//Draw a handle to identify the start scene
					if(scene.StartScene)
					{
						Vector3 startPos = new Vector3(window.xMax - 5, window.yMin + 5, 0);

						Handles.color = Color.gray;
						Handles.DrawSolidDisc(startPos, new Vector3(0, 0, 1), 9);

						Handles.color = Color.black;
						Handles.DrawSolidDisc(startPos, new Vector3(0, 0, 1), 8);

						Handles.color = Color.cyan;
						Handles.DrawSolidDisc(startPos, new Vector3(0, 0, 1), 7);
					}
				}

                EditorZoomArea.End();

            }
            GUILayout.EndVertical();
            //Set the scene layout rect to something reasonable
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (lastRect.x != 0) //Without this check sometimes the rect will be set to garbage
                SceneLayoutRect = lastRect;
        }

        /// <summary>
        /// Called from OnGUI
        /// For drawing "Scene Controls" such as adding scenes/characters
        /// </summary>
        void DrawSceneControls() 
        {
            //Control Area
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Add Scene"))
                {
                    NewSceneWizard.CreateWizard(CreateScene);
                }

                if (GUILayout.Button("Add Character"))
                {

                }
            }
            GUILayout.EndHorizontal();

			GUILayout.Space(6);
        }
        
        /// <summary>
        /// Called from OnGUI
        /// For drawing the scene "Inspector" which allows the user to edit a selected scene window
        /// </summary>
        void DrawSceneInspector() 
        {
            //Scene detail area
            GUILayout.BeginVertical(GUILayout.Width(300));
            {
                Scene selected = null;

                //If a scene is recently deleted we could have an out of range excpetion
                try
                {
                   selected = scenes[focusedWindowID - 1];
                }
                catch (System.ArgumentOutOfRangeException e) { }

                if(selected != null)
                {
                    //Draw the inspector for the Selected scene
					GUILayout.Label(selected.name);

					SceneEditor.DrawSceneInspector(selected);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear Selection"))
                    focusedWindowID = 0;

				GUILayout.Space (12);
            }
            GUILayout.EndVertical();   
        }

        void OnGUI()
        {
            HandleEvents();

            GUILayout.BeginHorizontal();
            {
				GUILayout.Space(6);

                //Scene Layout Area
                GUILayout.BeginVertical();
                {
                    DrawSceneLayout();

                    DrawSceneControls();
                }
                GUILayout.EndVertical();

                GUILayout.Space(12);

                DrawSceneInspector();

                GUILayout.Space(6);

            }
            GUILayout.EndHorizontal();
        
        }

        /// <summary>
        /// Draws a curve between two rects
        /// </summary>
        void DrawNodeCurve(Rect start, Rect end)
        {
            Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
            Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);

            Vector3 startTan = startPos + Vector3.right * 50;
            Vector3 endTan = endPos + Vector3.left * 50;

            Color shadowColor = new Color(0, 0, 0, 0.06f);

            //Draw shadow
            for (int i = 0; i < 3; i++)
                Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowColor, null, (i + 1) * 5);

            //Draw line
            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1);

            //Draw some doodads
            Handles.color = new Color(.4f, .4f, .4f);
            Handles.DrawSolidDisc(startPos, new Vector3(0, 0, 1), 5);

            Handles.color = new Color(.7f, .7f, .7f);
            Handles.DrawSolidDisc(endPos, new Vector3(0, 0, 1), 5);
        }

        /// <summary>
        /// Handles drawing of popup window inside editor window
        /// </summary>
        /// <param name="id"></param>
        void DrawNodeWindow(int id)
        {
            Scene scene = scenes[id - 1].GetComponent<Scene>();

			GUILayout.FlexibleSpace();

            if (Event.current.isMouse && Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                focusedWindowID = id;

            GUI.DragWindow();
        }

        /// <summary>
        /// Takes the VNKitScene object and adds it to the Overview System properly
        /// </summary>
        /// <param name="sceneObject">The created VNKitScene object</param>
        void CreateScene(GameObject sceneObject)
        {
            if (scenes.Count <= 0)
            {
                startScene = sceneObject;
                sceneObject.GetComponent<Scene>().StartScene = true;
            }

            Rect windowRect = new Rect(SceneLayoutRect.width / 2 - nodeWindowWidth / 2,
                                       SceneLayoutRect.height / 2 - nodeWindowHeight / 2,
                                       nodeWindowWidth,
                                       nodeWindowHeight);

            Scene scene = sceneObject.GetComponent<Scene>();

            scene.overviewX = windowRect.x;
            scene.overviewY = windowRect.y;

            scenes.Add(scene);
            nodeWindows.Add(windowRect);

            Repaint();
        }
    }

}