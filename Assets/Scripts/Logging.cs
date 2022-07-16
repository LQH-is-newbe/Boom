using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Logging : MonoBehaviour {
    private string filename;

    private void Awake() {
        if (!Debug.isDebugBuild) Application.logMessageReceived += Log;
        filename = Application.dataPath + "/Log.txt";
    }

    private void Log(string logString, string stackTrace, LogType type) {
        TextWriter tw = new StreamWriter(filename, true);

        tw.WriteLine("[" + System.DateTime.Now + "]" + logString);

        tw.Close();
    }
}
