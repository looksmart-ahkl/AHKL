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
// using System.Web;

public class Reference : MonoBehaviour {

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
			commandClip = Microphone.Start(null, false, 10, samplerate);  //Start recording (rewriting older recordings)
			Console.WriteLine("START 2");
		}


		if (Input.GetKeyUp (KeyCode.Space)) {
			Console.WriteLine("STOP 2");
			// Debug
			print("Thinking ...");

			// Save the audio file
			Microphone.End(null);
			SavAudio.Save("sample", commandClip);
			if (commandClip != null)
				Console.WriteLine("GOT IT 2");

			// At this point, we can delete the existing audio clip
			commandClip = null;

			//Grab the most up-to-date JSON file
			// url = "https://api.wit.ai/message?v=20160305&q=Put%20the%20box%20on%20the%20shelf";
			token = "NJP2HHQXIUK3IGW53WXL65NRD74GGJ5B";

			//Start a coroutine called "WaitForRequest" with that WWW variable passed in as an argument
			string witAiResponse = GetJSONText("Assets/sample.mp3");
			print (witAiResponse);
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
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.wit.ai/speech");

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

	void Handle(string textToParse) {

		print (textToParse);
		var N = JSON.Parse (textToParse);
		print ("SimpleJSON: " + N.ToString());

		string intent = N["outcomes"] [0] ["intent"].Value.ToLower ();

		// what function should I call?
		switch (intent)
		{
		case "move_object":
			print ("Intent is MOVE OBJECT");
			MoveObject(textToParse);
			break;
		case "create_object":
			print ("Intent is CREATE OBJECT");
			CreateObject (textToParse);
			break;
		default:
			print ("Sorry, didn't understand your intent.");
			break;
		}


	}

	void MoveObject(string textToParse){

		// Debug: print
		print ("MOVE OBJECT function called");

		// Parse JSON
		var N = JSON.Parse (textToParse);

		// Find the subject
		string subjJson = N["outcomes"][0]["entities"]["subject"][0]["value"].Value.ToLower();
		print ("Subject: " + subjJson);

		// Find the destination
		string destJson = N["outcomes"][0]["entities"]["destination"][0]["value"].Value.ToLower();
		print ("Destination: " + destJson);

		// Find the objects
		GameObject[] gameObjects = GenericFindObjects(new string[] {subjJson, destJson});
		GameObject subject = gameObjects [0];
		GameObject destination = gameObjects [1];

		// Find object's positions
		Vector3 subjectLoc = subject.transform.localPosition;
		string subjectLocDebug = subject.transform.localPosition.ToString ();
		print ("SubjectLoc: " + subjectLoc);

		Vector3 destLoc = destination.transform.localPosition + new Vector3 (0.0f, (destination.transform.lossyScale.y/2), 0.0f);
		string destLocDebug = destination.transform.localPosition.ToString ();

		// Now move the object
		StartCoroutine (MoveToPosition (subject, destLoc, moveTime));

	}

	void CreateObject(string textToParse){

		// Debug: print
		print ("CREATE OBJECT function called");

		// Parse JSON
		var N = JSON.Parse (textToParse);

		// Find the subject
		string subjJson = N["outcomes"][0]["entities"]["subject"][0]["value"].Value.ToLower();
		print ("Subject: " + subjJson);

		// Find the destination, if applicable
		string destJson = N["outcomes"][0]["entities"]["destination"][0]["value"].Value.ToLower();
		print ("Destination: " + destJson);

		// Find the objects
		GameObject[] gameObjects = GenericFindObjects(new string[] {subjJson, destJson});
		GameObject subject = gameObjects [0];
		GameObject destination = gameObjects [1];

		// if there's no destination:
		if (destJson == "") {
			InstantiateObject (subject, spawnPoint);
			// otherwise, instantiate at the specified destination
		} else {
			InstantiateObject (subject, destination);
		}


	}


	// A more generic function that takes an array of strings and returns an array of GameObjects

	GameObject[] GenericFindObjects(string[] objectsToFind) {

		print ("GENERIC FIND OBJECTS function called");
		print ("OBJECTS TO FIND: " + objectsToFind[0]);
		print ("OBJECTS TO FIND: " + objectsToFind[1]);

		// Create a GameObject array, the same size as our objectsToFind array
		GameObject[] gameObjectsArray = new GameObject[objectsToFind.Length];

		for (int i = 0; i < objectsToFind.Length; i++) {

			//debug 
			print("Loop #" + i);

			// if it can be found, add it to the array of GameObjects
			if (GameObject.Find (objectsToFind[i]) != null) {
				print("Trying to find " + objectsToFind [i]);
				gameObjectsArray[i] = GameObject.Find (objectsToFind [i]);
			}

			// otherwise, let us know
			else {
				print ("Couldn't find " + objectsToFind [i]);
			}

		}

		return gameObjectsArray;

	}

	// Coroutine: Move an object from one position to another
	IEnumerator MoveToPosition(GameObject subject, Vector3 newPosition, float time)
	{
		float elapsedTime = 0;
		Vector3 startingPos = subject.transform.position;
		while (elapsedTime < time)
		{
			print ("moving!");
			subject.transform.position = Vector3.Lerp(startingPos, newPosition, (elapsedTime / time));
			elapsedTime += Time.deltaTime;
			yield return null;
		}
	}

	// Coroutine: Instantiate an object at point
	IEnumerator InstantiateObject(GameObject subject, GameObject destination)
	{
		Vector3 instantiationPoint = destination.transform.position;
		Instantiate (subject, instantiationPoint, default(Quaternion));
		yield return null;
	}

	public enum JSONBinaryTag
	{
		Array = 1,
		Class = 2,
		Value = 3,
		IntValue = 4,
		DoubleValue = 5,
		BoolValue = 6,
		FloatValue = 7,
	}

	public abstract class JSONNode
	{
		#region common interface

		public virtual void Add (string aKey, JSONNode aItem)
		{
		}

		public virtual JSONNode this [int aIndex]   { get { return null; } set { } }

		public virtual JSONNode this [string aKey]  { get { return null; } set { } }

		public virtual string Value                { get { return ""; } set { } }

		public virtual int Count                   { get { return 0; } }

		public virtual void Add (JSONNode aItem)
		{
			Add ("", aItem);
		}

		public virtual JSONNode Remove (string aKey)
		{
			return null;
		}

		public virtual JSONNode Remove (int aIndex)
		{
			return null;
		}

		public virtual JSONNode Remove (JSONNode aNode)
		{
			return aNode;
		}

		public virtual IEnumerable<JSONNode> Children
		{
			get {
				yield break;
			}
		}

		public IEnumerable<JSONNode> DeepChildren
		{
			get {
				foreach (var C in Children)
					foreach (var D in C.DeepChildren)
						yield return D;
			}
		}

		public override string ToString ()
		{
			return "JSONNode";
		}

		public virtual string ToString (string aPrefix)
		{
			return "JSONNode";
		}

		public abstract string ToJSON (int prefix);

		#endregion common interface

		#region typecasting properties

		public virtual JSONBinaryTag Tag { get; set; }

		public virtual int AsInt
		{
			get {
				int v = 0;
				if (int.TryParse (Value, out v))
					return v;
				return 0;
			}
			set {
				Value = value.ToString ();
				Tag = JSONBinaryTag.IntValue;
			}
		}

		public virtual float AsFloat
		{
			get {
				float v = 0.0f;
				if (float.TryParse (Value, out v))
					return v;
				return 0.0f;
			}
			set {
				Value = value.ToString ();
				Tag = JSONBinaryTag.FloatValue;
			}
		}

		public virtual double AsDouble
		{
			get {
				double v = 0.0;
				if (double.TryParse (Value, out v))
					return v;
				return 0.0;
			}
			set {
				Value = value.ToString ();
				Tag = JSONBinaryTag.DoubleValue;

			}
		}

		public virtual bool AsBool
		{
			get {
				bool v = false;
				if (bool.TryParse (Value, out v))
					return v;
				return !string.IsNullOrEmpty (Value);
			}
			set {
				Value = (value) ? "true" : "false";
				Tag = JSONBinaryTag.BoolValue;

			}
		}

		public virtual JSONArray AsArray
		{
			get {
				return this as JSONArray;
			}
		}

		public virtual JSONClass AsObject
		{
			get {
				return this as JSONClass;
			}
		}


		#endregion typecasting properties

		#region operators

		public static implicit operator JSONNode (string s)
		{
			return new JSONData (s);
		}

		public static implicit operator string (JSONNode d)
		{
			return (d == null) ? null : d.Value;
		}

		public static bool operator == (JSONNode a, object b)
		{
			if (b == null && a is JSONLazyCreator)
				return true;
			return System.Object.ReferenceEquals (a, b);
		}

		public static bool operator != (JSONNode a, object b)
		{
			return !(a == b);
		}

		public override bool Equals (object obj)
		{
			return System.Object.ReferenceEquals (this, obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}


		#endregion operators

		internal static string Escape (string aText)
		{
			string result = "";
			foreach (char c in aText) {
				switch (c) {
				case '\\':
					result += "\\\\";
					break;
				case '\"':
					result += "\\\"";
					break;
				case '\n':
					result += "\\n";
					break;
				case '\r':
					result += "\\r";
					break;
				case '\t':
					result += "\\t";
					break;
				case '\b':
					result += "\\b";
					break;
				case '\f':
					result += "\\f";
					break;
				default   :
					result += c;
					break;
				}
			}
			return result;
		}

		static JSONData Numberize (string token)
		{
			bool flag = false;
			int integer = 0;
			double real = 0;

			if (int.TryParse (token, out integer)) {
				return new JSONData (integer);
			}

			if (double.TryParse (token, out real)) {
				return new JSONData (real);
			}

			if (bool.TryParse (token, out flag)) {
				return new JSONData (flag);
			}

			throw new NotImplementedException (token);
		}

		static void AddElement (JSONNode ctx, string token, string tokenName, bool tokenIsString)
		{
			if (tokenIsString) {
				if (ctx is JSONArray)
					ctx.Add (token);
				else
					ctx.Add (tokenName, token); // assume dictionary/object
			} else {
				JSONData number = Numberize (token);
				if (ctx is JSONArray)
					ctx.Add (number);
				else
					ctx.Add (tokenName, number);

			}
		}

		public static JSONNode Parse (string aJSON)
		{
			Stack<JSONNode> stack = new Stack<JSONNode> ();
			JSONNode ctx = null;
			int i = 0;
			string Token = "";
			string TokenName = "";
			bool QuoteMode = false;
			bool TokenIsString = false;
			while (i < aJSON.Length) {
				switch (aJSON [i]) {
				case '{':
					if (QuoteMode) {
						Token += aJSON [i];
						break;
					}
					stack.Push (new JSONClass ());
					if (ctx != null) {
						TokenName = TokenName.Trim ();
						if (ctx is JSONArray)
							ctx.Add (stack.Peek ());
						else if (TokenName != "")
							ctx.Add (TokenName, stack.Peek ());
					}
					TokenName = "";
					Token = "";
					ctx = stack.Peek ();
					break;

				case '[':
					if (QuoteMode) {
						Token += aJSON [i];
						break;
					}

					stack.Push (new JSONArray ());
					if (ctx != null) {
						TokenName = TokenName.Trim ();

						if (ctx is JSONArray)
							ctx.Add (stack.Peek ());
						else if (TokenName != "")
							ctx.Add (TokenName, stack.Peek ());
					}
					TokenName = "";
					Token = "";
					ctx = stack.Peek ();
					break;

				case '}':
				case ']':
					if (QuoteMode) {
						Token += aJSON [i];
						break;
					}
					if (stack.Count == 0)
						throw new Exception ("JSON Parse: Too many closing brackets");

					stack.Pop ();
					if (Token != "") {
						TokenName = TokenName.Trim ();
						/*
							if (ctx is JSONArray)
								ctx.Add (Token);
							else if (TokenName != "")
								ctx.Add (TokenName, Token);
								*/
						AddElement (ctx, Token, TokenName, TokenIsString);
						TokenIsString = false;
					}
					TokenName = "";
					Token = "";
					if (stack.Count > 0)
						ctx = stack.Peek ();
					break;

				case ':':
					if (QuoteMode) {
						Token += aJSON [i];
						break;
					}
					TokenName = Token;
					Token = "";
					TokenIsString = false;
					break;

				case '"':
					QuoteMode ^= true;
					TokenIsString = QuoteMode == true ? true : TokenIsString;
					break;

				case ',':
					if (QuoteMode) {
						Token += aJSON [i];
						break;
					}
					if (Token != "") {
						/*
							if (ctx is JSONArray) {
								ctx.Add (Token);
							} else if (TokenName != "") {
								ctx.Add (TokenName, Token);
							}
							*/
						AddElement (ctx, Token, TokenName, TokenIsString);
						TokenIsString = false;

					}
					TokenName = "";
					Token = "";
					TokenIsString = false;
					break;

				case '\r':
				case '\n':
					break;

				case ' ':
				case '\t':
					if (QuoteMode)
						Token += aJSON [i];
					break;

				case '\\':
					++i;
					if (QuoteMode) {
						char C = aJSON [i];
						switch (C) {
						case 't':
							Token += '\t';
							break;
						case 'r':
							Token += '\r';
							break;
						case 'n':
							Token += '\n';
							break;
						case 'b':
							Token += '\b';
							break;
						case 'f':
							Token += '\f';
							break;
						case 'u':
							{
								string s = aJSON.Substring (i + 1, 4);
								Token += (char)int.Parse (
									s,
									System.Globalization.NumberStyles.AllowHexSpecifier);
								i += 4;
								break;
							}
						default  :
							Token += C;
							break;
						}
					}
					break;

				default:
					Token += aJSON [i];
					break;
				}
				++i;
			}
			if (QuoteMode) {
				throw new Exception ("JSON Parse: Quotation marks seems to be messed up.");
			}
			return ctx;
		}

		public virtual void Serialize (System.IO.BinaryWriter aWriter)
		{
		}

		public void SaveToStream (System.IO.Stream aData)
		{
			var W = new System.IO.BinaryWriter (aData);
			Serialize (W);
		}

		#if USE_SharpZipLib
		public void SaveToCompressedStream(System.IO.Stream aData)
		{
		using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
		{
		gzipOut.IsStreamOwner = false;
		SaveToStream(gzipOut);
		gzipOut.Close();
		}
		}

		public void SaveToCompressedFile(string aFileName)
		{

		#if USE_FileIO
		System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
		using(var F = System.IO.File.OpenWrite(aFileName))
		{
		SaveToCompressedStream(F);
		}

		#else
		throw new Exception("Can't use File IO stuff in webplayer");
		#endif
	}
	public string SaveToCompressedBase64()
	{
		using (var stream = new System.IO.MemoryStream())
		{
			SaveToCompressedStream(stream);
			stream.Position = 0;
			return System.Convert.ToBase64String(stream.ToArray());
		}
	}

		#else
	public void SaveToCompressedStream (System.IO.Stream aData)
	{
		throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public void SaveToCompressedFile (string aFileName)
	{
		throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}

	public string SaveToCompressedBase64 ()
	{
		throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
	}
		#endif

	public void SaveToFile (string aFileName)
	{
		#if USE_FileIO
		System.IO.Directory.CreateDirectory ((new System.IO.FileInfo (aFileName)).Directory.FullName);
		using (var F = System.IO.File.OpenWrite (aFileName)) {
		SaveToStream (F);
		}
		#else
		throw new Exception ("Can't use File IO stuff in webplayer");
		#endif
	}

	public string SaveToBase64 ()
	{
		using (var stream = new System.IO.MemoryStream ()) {
			SaveToStream (stream);
			stream.Position = 0;
			return System.Convert.ToBase64String (stream.ToArray ());
		}
	}

	public static JSONNode Deserialize (System.IO.BinaryReader aReader)
	{
		JSONBinaryTag type = (JSONBinaryTag)aReader.ReadByte ();
		switch (type) {
		case JSONBinaryTag.Array:
			{
				int count = aReader.ReadInt32 ();
				JSONArray tmp = new JSONArray ();
				for (int i = 0; i < count; i++)
					tmp.Add (Deserialize (aReader));
				return tmp;
			}
		case JSONBinaryTag.Class:
			{
				int count = aReader.ReadInt32 ();                
				JSONClass tmp = new JSONClass ();
				for (int i = 0; i < count; i++) {
					string key = aReader.ReadString ();
					var val = Deserialize (aReader);
					tmp.Add (key, val);
				}
				return tmp;
			}
		case JSONBinaryTag.Value:
			{
				return new JSONData (aReader.ReadString ());
			}
		case JSONBinaryTag.IntValue:
			{
				return new JSONData (aReader.ReadInt32 ());
			}
		case JSONBinaryTag.DoubleValue:
			{
				return new JSONData (aReader.ReadDouble ());
			}
		case JSONBinaryTag.BoolValue:
			{
				return new JSONData (aReader.ReadBoolean ());
			}
		case JSONBinaryTag.FloatValue:
			{
				return new JSONData (aReader.ReadSingle ());
			}

		default:
			{
				throw new Exception ("Error deserializing JSON. Unknown tag: " + type);
			}
		}
	}

	#if USE_SharpZipLib
	public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
	{
	var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
	return LoadFromStream(zin);
	}
	public static JSONNode LoadFromCompressedFile(string aFileName)
	{
	#if USE_FileIO
	using(var F = System.IO.File.OpenRead(aFileName))
	{
	return LoadFromCompressedStream(F);
	}
	#else
	throw new Exception("Can't use File IO stuff in webplayer");
	#endif
}
public static JSONNode LoadFromCompressedBase64(string aBase64)
{
	var tmp = System.Convert.FromBase64String(aBase64);
	var stream = new System.IO.MemoryStream(tmp);
	stream.Position = 0;
	return LoadFromCompressedStream(stream);
}
	#else
public static JSONNode LoadFromCompressedFile (string aFileName)
{
	throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
}

public static JSONNode LoadFromCompressedStream (System.IO.Stream aData)
{
	throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
}

public static JSONNode LoadFromCompressedBase64 (string aBase64)
{
	throw new Exception ("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
}
	#endif

public static JSONNode LoadFromStream (System.IO.Stream aData)
{
	using (var R = new System.IO.BinaryReader (aData)) {
		return Deserialize (R);
	}
}

public static JSONNode LoadFromFile (string aFileName)
{
	#if USE_FileIO
	using (var F = System.IO.File.OpenRead (aFileName)) {
	return LoadFromStream (F);
	}
	#else
	throw new Exception ("Can't use File IO stuff in webplayer");
	#endif
}

public static JSONNode LoadFromBase64 (string aBase64)
{
	var tmp = System.Convert.FromBase64String (aBase64);
	var stream = new System.IO.MemoryStream (tmp);
	stream.Position = 0;
	return LoadFromStream (stream);
}
}
// End of JSONNode

public class JSONArray : JSONNode, IEnumerable
{
	private List<JSONNode> m_List = new List<JSONNode> ();

	public override JSONNode this [int aIndex]
	{
		get {
			if (aIndex < 0 || aIndex >= m_List.Count)
				return new JSONLazyCreator (this);
			return m_List [aIndex];
		}
		set {
			if (aIndex < 0 || aIndex >= m_List.Count)
				m_List.Add (value);
			else
				m_List [aIndex] = value;
		}
	}

	public override JSONNode this [string aKey]
	{
		get{ return new JSONLazyCreator (this); }
		set{ m_List.Add (value); }
	}

	public override int Count
	{
		get { return m_List.Count; }
	}

	public override void Add (string aKey, JSONNode aItem)
	{
		m_List.Add (aItem);
	}

	public override JSONNode Remove (int aIndex)
	{
		if (aIndex < 0 || aIndex >= m_List.Count)
			return null;
		JSONNode tmp = m_List [aIndex];
		m_List.RemoveAt (aIndex);
		return tmp;
	}

	public override JSONNode Remove (JSONNode aNode)
	{
		m_List.Remove (aNode);
		return aNode;
	}

	public override IEnumerable<JSONNode> Children
	{
		get {
			foreach (JSONNode N in m_List)
				yield return N;
		}
	}

	public IEnumerator GetEnumerator ()
	{
		foreach (JSONNode N in m_List)
			yield return N;
	}

	public override string ToString ()
	{
		string result = "[ ";
		foreach (JSONNode N in m_List) {
			if (result.Length > 2)
				result += ", ";
			result += N.ToString ();
		}
		result += " ]";
		return result;
	}

	public override string ToString (string aPrefix)
	{
		string result = "[ ";
		foreach (JSONNode N in m_List) {
			if (result.Length > 3)
				result += ", ";
			result += "\n" + aPrefix + "   ";                
			result += N.ToString (aPrefix + "   ");
		}
		result += "\n" + aPrefix + "]";
		return result;
	}

	public override string ToJSON (int prefix)
	{
		string s = new string (' ', (prefix + 1) * 2);
		string ret = "[ ";
		foreach (JSONNode n in m_List) {
			if (ret.Length > 3)
				ret += ", ";
			ret += "\n" + s;
			ret += n.ToJSON (prefix + 1);

		}
		ret += "\n" + s + "]";
		return ret;
	}

	public override void Serialize (System.IO.BinaryWriter aWriter)
	{
		aWriter.Write ((byte)JSONBinaryTag.Array);
		aWriter.Write (m_List.Count);
		for (int i = 0; i < m_List.Count; i++) {
			m_List [i].Serialize (aWriter);
		}
	}
}
// End of JSONArray

public class JSONClass : JSONNode, IEnumerable
{
	private Dictionary<string,JSONNode> m_Dict = new Dictionary<string,JSONNode> ();

	public override JSONNode this [string aKey]
	{
		get {
			if (m_Dict.ContainsKey (aKey))
				return m_Dict [aKey];
			else
				return new JSONLazyCreator (this, aKey);
		}
		set {
			if (m_Dict.ContainsKey (aKey))
				m_Dict [aKey] = value;
			else
				m_Dict.Add (aKey, value);
		}
	}

	public override JSONNode this [int aIndex]
	{
		get {
			if (aIndex < 0 || aIndex >= m_Dict.Count)
				return null;
			return m_Dict.ElementAt (aIndex).Value;
		}
		set {
			if (aIndex < 0 || aIndex >= m_Dict.Count)
				return;
			string key = m_Dict.ElementAt (aIndex).Key;
			m_Dict [key] = value;
		}
	}

	public override int Count
	{
		get { return m_Dict.Count; }
	}


	public override void Add (string aKey, JSONNode aItem)
	{
		if (!string.IsNullOrEmpty (aKey)) {
			if (m_Dict.ContainsKey (aKey))
				m_Dict [aKey] = aItem;
			else
				m_Dict.Add (aKey, aItem);
		} else
			m_Dict.Add (Guid.NewGuid ().ToString (), aItem);
	}

	public override JSONNode Remove (string aKey)
	{
		if (!m_Dict.ContainsKey (aKey))
			return null;
		JSONNode tmp = m_Dict [aKey];
		m_Dict.Remove (aKey);
		return tmp;        
	}

	public override JSONNode Remove (int aIndex)
	{
		if (aIndex < 0 || aIndex >= m_Dict.Count)
			return null;
		var item = m_Dict.ElementAt (aIndex);
		m_Dict.Remove (item.Key);
		return item.Value;
	}

	public override JSONNode Remove (JSONNode aNode)
	{
		try {
			var item = m_Dict.Where (k => k.Value == aNode).First ();
			m_Dict.Remove (item.Key);
			return aNode;
		} catch {
			return null;
		}
	}

	public override IEnumerable<JSONNode> Children
	{
		get {
			foreach (KeyValuePair<string,JSONNode> N in m_Dict)
				yield return N.Value;
		}
	}

	public IEnumerator GetEnumerator ()
	{
		foreach (KeyValuePair<string, JSONNode> N in m_Dict)
			yield return N;
	}

	public override string ToString ()
	{
		string result = "{";
		foreach (KeyValuePair<string, JSONNode> N in m_Dict) {
			if (result.Length > 2)
				result += ", ";
			result += "\"" + Escape (N.Key) + "\":" + N.Value.ToString ();
		}
		result += "}";
		return result;
	}

	public override string ToString (string aPrefix)
	{
		string result = "{ ";
		foreach (KeyValuePair<string, JSONNode> N in m_Dict) {
			if (result.Length > 3)
				result += ", ";
			result += "\n" + aPrefix + "   ";
			result += "\"" + Escape (N.Key) + "\" : " + N.Value.ToString (aPrefix + "   ");
		}
		result += "\n" + aPrefix + "}";
		return result;
	}

	public override string ToJSON (int prefix)
	{
		string s = new string (' ', (prefix + 1) * 2);
		string ret = "{ ";
		foreach (KeyValuePair<string,JSONNode> n in m_Dict) {
			if (ret.Length > 3)
				ret += ", ";
			ret += "\n" + s;
			ret += string.Format ("\"{0}\": {1}", n.Key, n.Value.ToJSON (prefix + 1));
		}
		ret += "\n" + s + "}";
		return ret;
	}

	public override void Serialize (System.IO.BinaryWriter aWriter)
	{
		aWriter.Write ((byte)JSONBinaryTag.Class);
		aWriter.Write (m_Dict.Count);
		foreach (string K in m_Dict.Keys) {
			aWriter.Write (K);
			m_Dict [K].Serialize (aWriter);
		}
	}
}
// End of JSONClass

public class JSONData : JSONNode
{
	private string m_Data;


	public override string Value
	{
		get { return m_Data; }
		set {
			m_Data = value;
			Tag = JSONBinaryTag.Value;
		}
	}

	public JSONData (string aData)
	{
		m_Data = aData;
		Tag = JSONBinaryTag.Value;
	}

	public JSONData (float aData)
	{
		AsFloat = aData;
	}

	public JSONData (double aData)
	{
		AsDouble = aData;
	}

	public JSONData (bool aData)
	{
		AsBool = aData;
	}

	public JSONData (int aData)
	{
		AsInt = aData;
	}

	public override string ToString ()
	{
		return "\"" + Escape (m_Data) + "\"";
	}

	public override string ToString (string aPrefix)
	{
		return "\"" + Escape (m_Data) + "\"";
	}

	public override string ToJSON (int prefix)
	{
		switch (Tag) {
		case JSONBinaryTag.DoubleValue:
		case JSONBinaryTag.FloatValue:
		case JSONBinaryTag.IntValue:
			return m_Data;
		case JSONBinaryTag.Value:
			return string.Format ("\"{0}\"", Escape (m_Data));
		default:
			throw new NotSupportedException ("This shouldn't be here: " + Tag.ToString ());
		}
	}

	public override void Serialize (System.IO.BinaryWriter aWriter)
	{
		var tmp = new JSONData ("");

		tmp.AsInt = AsInt;
		if (tmp.m_Data == this.m_Data) {
			aWriter.Write ((byte)JSONBinaryTag.IntValue);
			aWriter.Write (AsInt);
			return;
		}
		tmp.AsFloat = AsFloat;
		if (tmp.m_Data == this.m_Data) {
			aWriter.Write ((byte)JSONBinaryTag.FloatValue);
			aWriter.Write (AsFloat);
			return;
		}
		tmp.AsDouble = AsDouble;
		if (tmp.m_Data == this.m_Data) {
			aWriter.Write ((byte)JSONBinaryTag.DoubleValue);
			aWriter.Write (AsDouble);
			return;
		}

		tmp.AsBool = AsBool;
		if (tmp.m_Data == this.m_Data) {
			aWriter.Write ((byte)JSONBinaryTag.BoolValue);
			aWriter.Write (AsBool);
			return;
		}
		aWriter.Write ((byte)JSONBinaryTag.Value);
		aWriter.Write (m_Data);
	}
}
// End of JSONData

internal class JSONLazyCreator : JSONNode
{
	private JSONNode m_Node = null;
	private string m_Key = null;

	public JSONLazyCreator (JSONNode aNode)
	{
		m_Node = aNode;
		m_Key = null;
	}

	public JSONLazyCreator (JSONNode aNode, string aKey)
	{
		m_Node = aNode;
		m_Key = aKey;
	}

	private void Set (JSONNode aVal)
	{
		if (m_Key == null) {
			m_Node.Add (aVal);
		} else {
			m_Node.Add (m_Key, aVal);
		}
		m_Node = null; // Be GC friendly.
	}

	public override JSONNode this [int aIndex]
	{
		get {
			return new JSONLazyCreator (this);
		}
		set {
			var tmp = new JSONArray ();
			tmp.Add (value);
			Set (tmp);
		}
	}

	public override JSONNode this [string aKey]
	{
		get {
			return new JSONLazyCreator (this, aKey);
		}
		set {
			var tmp = new JSONClass ();
			tmp.Add (aKey, value);
			Set (tmp);
		}
	}

	public override void Add (JSONNode aItem)
	{
		var tmp = new JSONArray ();
		tmp.Add (aItem);
		Set (tmp);
	}

	public override void Add (string aKey, JSONNode aItem)
	{
		var tmp = new JSONClass ();
		tmp.Add (aKey, aItem);
		Set (tmp);
	}

	public static bool operator == (JSONLazyCreator a, object b)
	{
		if (b == null)
			return true;
		return System.Object.ReferenceEquals (a, b);
	}

	public static bool operator != (JSONLazyCreator a, object b)
	{
		return !(a == b);
	}

	public override bool Equals (object obj)
	{
		if (obj == null)
			return true;
		return System.Object.ReferenceEquals (this, obj);
	}

	public override int GetHashCode ()
	{
		return base.GetHashCode ();
	}

	public override string ToString ()
	{
		return "";
	}

	public override string ToString (string aPrefix)
	{
		return "";
	}

	public override string ToJSON (int prefix)
	{
		return "";
	}

	public override int AsInt
	{
		get {
			JSONData tmp = new JSONData (0);
			Set (tmp);
			return 0;
		}
		set {
			JSONData tmp = new JSONData (value);
			Set (tmp);
		}
	}

	public override float AsFloat
	{
		get {
			JSONData tmp = new JSONData (0.0f);
			Set (tmp);
			return 0.0f;
		}
		set {
			JSONData tmp = new JSONData (value);
			Set (tmp);
		}
	}

	public override double AsDouble
	{
		get {
			JSONData tmp = new JSONData (0.0);
			Set (tmp);
			return 0.0;
		}
		set {
			JSONData tmp = new JSONData (value);
			Set (tmp);
		}
	}

	public override bool AsBool
	{
		get {
			JSONData tmp = new JSONData (false);
			Set (tmp);
			return false;
		}
		set {
			JSONData tmp = new JSONData (value);
			Set (tmp);
		}
	}

	public override JSONArray AsArray
	{
		get {
			JSONArray tmp = new JSONArray ();
			Set (tmp);
			return tmp;
		}
	}

	public override JSONClass AsObject
	{
		get {
			JSONClass tmp = new JSONClass ();
			Set (tmp);
			return tmp;
		}
	}
}
// End of JSONLazyCreator

public static class JSON
{
	public static JSONNode Parse (string aJSON)
	{
		return JSONNode.Parse (aJSON);
	}
}

}