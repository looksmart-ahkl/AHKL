﻿/***********************************************************************************
MIT License

Copyright (c) 2016 Aaron Faucher

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE.

***********************************************************************************/
using UnityEngine;
using UnityEngine.Experimental.Networking;
using System.Collections;
//using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;


public partial class Wit : MonoBehaviour {

	// Class Variables

	// Audio variables
	public AudioClip commandClip;
	int samplerate;

	// API access parameters
	string url;
	string token;
	UnityWebRequest wr;

	// Movement variables
	public float moveTime;
	public float yOffset;

	// GameObject to use as a default spawn point
	public GameObject spawnPoint;

	// Use this for initialization
	void Start () {

		// If you are a Windows user and receiving a Tlserror
		// See: https://github.com/afauch/wit3d/issues/2
		// Uncomment the line below to bypass SSL
		// System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => { return true; };

		// set samplerate to 16000 for wit.ai
		samplerate = 16000;

	}

	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown (KeyCode.Space)) {
			print ("Listening for command");
			commandClip = Microphone.Start(null, false, 5, samplerate);  //Start recording (rewriting older recordings)
			Console.WriteLine("START");
		}


		if (Input.GetKeyUp (KeyCode.Space)) {
			Console.WriteLine("STOP");
			// Debug
			print("Thinking ...");

			// Save the audio file
			Microphone.End(null);
			SavAudio.Save("sample", commandClip);
			if (commandClip != null)
				Console.WriteLine("GOT IT");
			
			// At this point, we can delete the existing audio clip
			commandClip = null;

			//Grab the most up-to-date JSON file
			// url = "https://api.wit.ai/message?v=20160305&q=Put%20the%20box%20on%20the%20shelf";
			token = "NJP2HHQXIUK3IGW53WXL65NRD74GGJ5B";



			//Start a coroutine called "WaitForRequest" with that WWW variable passed in as an argument
			string witAiResponse = GetJSONText("Assets/sample.mp3");
			//print (witAiResponse);
			Handle (witAiResponse);
		}


	}

	string GetJSONText(string file) {

		// get the file w/ FileStream
		FileStream filestream = new FileStream (file, FileMode.Open, FileAccess.Read);
		BinaryReader filereader = new BinaryReader (filestream);
		byte[] BA_AudioFile = filereader.ReadBytes ((Int32)filestream.Length);
		filestream.Close ();
		filereader.Close ();

		// create an HttpWebRequest
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://x.org.my?action=process-request");

		request.Method = "POST";
		request.Headers ["Authorization"] = "Bearer " + token;
		request.ContentType = "audio/wav";
		request.ContentLength = BA_AudioFile.Length;
		request.GetRequestStream ().Write (BA_AudioFile, 0, BA_AudioFile.Length);

		// Process the wit.ai response
		try
		{
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				print("Http went through ok");
				StreamReader response_stream = new StreamReader(response.GetResponseStream());
				return response_stream.ReadToEnd();
			}
			else
			{
				return "Error: " + response.StatusCode.ToString();
				return "HTTP ERROR";
			}
		}
		catch (Exception ex)
		{
			return "Error: " + ex.Message;
			return "HTTP ERROR";
		}       
	}

}


