﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using VVVV.Core.Logging;
using VVVV.Core.Model.FX;

using System.Text.RegularExpressions;

namespace VVVV.HDE.CodeEditor.LanguageBindings.FX
{
    public class FXCompletionProvider : DefaultCompletionProvider
    {
        protected CodeEditor FEditor;
        protected ILogger FLogger;
        protected Dictionary<string, string> FHLSLReference;
        protected Dictionary<string, string> FTypeReference;
        
        public FXCompletionProvider(CodeEditor editor)
        {
            FEditor = editor;
            FLogger = editor.Logger;
            
            FHLSLReference = new Dictionary<string, string>();
            FTypeReference = new Dictionary<string, string>();
            
            ParseHLSLFunctionReference();
            
            AddTypeToReference("float");
            AddTypeToReference("int");
            AddTypeToReference("bool");
            FTypeReference.Add("float3x4", "");
            FTypeReference.Add("float4x3", "");
        }
        
        private void AddTypeToReference(string type)
        {
            for (int i = 0; i <= 4; i++)
            {
                if (i == 0)
                    FTypeReference.Add(type, "");
                else if (i > 1)
                    FTypeReference.Add(type + i.ToString(), "");
            }
        }
        
        private void ParseHLSLFunctionReference()
        {
            try
            {
                var functionReference = new XmlDocument();
                var filename = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), @"..\bin\hlsl.fnr"));
                functionReference.Load(filename);
                foreach (XmlNode function in functionReference.DocumentElement.ChildNodes)
                {
                    var item = new List<string>();
                    foreach (XmlNode cell in function)
                        item.Add(cell.InnerText);
                    
                    //only take functions for shadermodels < 3
                    int sm = Convert.ToInt32(item[2][0].ToString());
                    if (sm < 4)
                        FHLSLReference.Add(item[0], item[1] + "\nMinimum required ShaderModel: " + item[2]);
                }
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Error, @"Error parsing HLSL function reference \effects\hlsl.fnr");
                FLogger.Log(e);
            }
        }
        
        public override ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            // We can return code-completion items like this:
            
            //return new ICompletionData[] {
            //	new DefaultCompletionData("Text", "Description", 1)
            //};
            
            //types: everywhere
            //variable names: only inside of single {}
            //hlsl reference: only inside of single {}
            //semantics: only directly after :
            
            var tempDict = new Dictionary<string, DefaultCompletionData>();
            
            //parse ParameterDescription
            var doc = FEditor.TextDocument;
            var project = doc.Project as FXProject;
            if (project == null)
                return new ICompletionData[0];
            
            var inputs = project.ParameterDescription.Split(new char[1]{'|'});
            foreach (var input in inputs)
            {
                if (!string.IsNullOrEmpty(input))
                {
                    var desc = input.Split(new char[1]{','});
                    string name = "";
                    if (desc[0] != desc[1])
                        name = desc[1] + "\n";
                    
                    string tooltip = name + desc[2] + "\n";
                    if (Convert.ToInt32(desc[3]) > 1)
                        tooltip += desc[6].Replace("(Rows)", "") + desc[3] + "x" + desc[4];
                    else
                        tooltip += desc[7] + desc[4].Replace("1", "").Replace("0", "");
                    tempDict.Add(desc[0].ToLower(), new DefaultCompletionData(desc[0], tooltip, 3));
                }
            }
            
            foreach(var function in FHLSLReference)
            {
                var pos = function.Key.IndexOf("(");
                var text = function.Key;
                 if (pos >= 0)
                    text = function.Key.Substring(0, pos);
                if (!tempDict.ContainsKey(text.ToLower()))
                    tempDict.Add(text.ToLower(), new DefaultCompletionData(text, function.Key + "\n" + function.Value, 1));
            }
            
            foreach(var type in FTypeReference)
                tempDict.Add(type.Key.ToLower(), new DefaultCompletionData(type.Key, type.Value, 0));
            
            //add all words of the text not part of a comment and not yet in the dict                             
            Regex rx = new Regex(@"^[A-Za-z_]\w+$");
            foreach(var line in textArea.Document.LineSegmentCollection)
            {
                var l = line.Words.ToArray();
                if (l.Length > 0)
                {
                    if (!l[0].Word.StartsWith("//"))
                        foreach(var word in line.Words)
                    {
                        if (word.Word.StartsWith("//"))
                            break;
                        else if (!tempDict.ContainsKey(word.Word.ToLower()) && rx.IsMatch(word.Word))
                            tempDict.Add(word.Word.ToLower(), new DefaultCompletionData(word.Word, "", 2));
                    }
                }
            }
            
            //add a, r, g, b, x, y, z, w manually
            var components = new string[8] {"a", "r", "g", "b", "x", "y", "z", "w"};
            for(int i = 0; i < components.Length; i++)
                tempDict.Add(components[i], new DefaultCompletionData(components[i], "", 2));

            //move all completion data to the output array
            ICompletionData[] cData = new ICompletionData[tempDict.Count];
            var j = 0;
            foreach(var temp in tempDict)
            {
                cData[j] = temp.Value;
                j++;
            }
            
            //set preselection to "" for popup to sort by first character
            PreSelection = "";
            
            return cData;
        }
    }
}
