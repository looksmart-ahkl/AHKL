/***********************************************************************************
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
using System.Threading;
using HttpUtils;
using LitJson;


public partial class Wit : MonoBehaviour {

	// Class Variables

	// Audio variables
	public AudioClip commandClip;
	int samplerate;
	int waitDuration;

	// API access parameters
	string url;
	string token;
	UnityWebRequest wr;

	// Movement variables
	public float moveTime;
	public float yOffset;
	public bool isThreadRunning = false;
	private int running;

	// GameObject to use as a default spawn point
	public GameObject spawnPoint;

	private string jsonString;
	private JsonData itemData;

	private string display;

	public TextMesh displayLabel;

	// Use this for initialization
	void Start () {
		waitDuration = 5;
		samplerate = 16000;
	}

	// Update is called once per frame
	void Update () {
		/*if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
		{
			print ("New thread");
			Thread t = new Thread
				(
					() =>
					{
						try
						{
							RecordAudio(5);
						}
						catch
						{
							//Without the catch any exceptions will be unhandled
							//(Maybe that's what you want, maybe not*)
						}
						finally
						{
							//Regardless of exceptions, we need this to happen:
							running = 0;
						}
					}
				);
			t.IsBackground = true;
			t.Name = "rAudio";
			print ("New thread starting");
			t.Start();
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("rAudio is already Running.");
			print ("Thread exist");
		}*/   

		/*if (!isThreadRunning) {
			var rAudio = new Thread (RecordAudio(5));
			rAudio.IsBackground = true;
			rAudio.Name = "rAudio";
			rAudio.Start ();
		}*/

		RecordAudio (waitDuration);
	}

	IEnumerator Wait(int duration)
	{
		print ("Start waiting sequence");
		yield return new WaitForSeconds(duration);   //Wait
		print ("Waiting sequence ends");
	}

	void RecordAudio(int duration)
	{		
		if (Input.GetKeyDown (KeyCode.Space)) {
			print ("Start Recording");
			commandClip = Microphone.Start (null, false, 3, samplerate);  //Start recording (rewriting older recordings)
		Wait(360);
		this.Show();		
	}

		if (Input.GetKeyUp (KeyCode.Space)) {
			print ("Stop Recording");
			// Save the audio file
			Microphone.End (null);
			SavAudio.Save ("sample", commandClip);

			// At this point, we can delete the existing audio clip
			commandClip = null;

			//Grab the most up-to-date JSON file
			// url = "https://api.wit.ai/message?v=20160305&q=Put%20the%20box%20on%20the%20shelf";
			token = "NJP2HHQXIUK3IGW53WXL65NRD74GGJ5B";

		this.HttpUploadFile("http://x.org.my?action=process-request", 
			@"Assets/sample.wav", "file", "audio/wav");
			//Start a coroutine called "WaitForRequest" with that WWW variable passed in as an argument
			//string witAiResponse = GetJSONText ("Assets/sample.wav");
		}
	}
	
public void HttpUploadFile(string url, string file, string paramName, string contentType) {
	string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
	byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

	HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
	wr.ContentType = "multipart/form-data; boundary=" + boundary;
	wr.Method = "POST";
	wr.KeepAlive = true;
	wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

	Stream rs = wr.GetRequestStream();

	string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

	rs.Write(boundarybytes, 0, boundarybytes.Length);

	string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
	string header = string.Format(headerTemplate, paramName, file, contentType);
	byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
	rs.Write(headerbytes, 0, headerbytes.Length);

	FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
	byte[] buffer = new byte[4096];
	int bytesRead = 0;
	while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
		rs.Write(buffer, 0, bytesRead);
	}
	fileStream.Close();

	byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
	rs.Write(trailer, 0, trailer.Length);
	rs.Close();
	print("print1");

	WebResponse wresp = null;
	try {
		print("print2");
		wresp = wr.GetResponse();
		Stream stream2 = wresp.GetResponseStream();
		StreamReader reader2 = new StreamReader(stream2);
		print("print3 " + reader2.ReadToEnd());
		string jsonStr = reader2.ReadToEnd();
	} catch(Exception ex) {
		print("print4: " + ex.Message);
		if(wresp != null) {
			wresp.Close();
			wresp = null;
			print("print5");
		}
	} finally {
		wr = null;
		print("print6");
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
	print("Upload file");
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://x.org.my?action=process-request");
		
		request.Method = "POST";
		request.ContentType = "multipart/form-data";
		request.KeepAlive = true;

		Stream rs = request.GetRequestStream ();



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

public void Show () {

	jsonString = File.ReadAllText (Application.dataPath + "/recipe.json");
	itemData = JsonMapper.ToObject (jsonString);

	string display = (string) itemData ["response"] ["text"];
	print (display);

	displayLabel = (TextMesh)GameObject.Find ("label").GetComponent<TextMesh>();



	// here we change the value of displayed text
	displayLabel.text = display;
	//StartCoroutine (LoadImg ());
}



}


