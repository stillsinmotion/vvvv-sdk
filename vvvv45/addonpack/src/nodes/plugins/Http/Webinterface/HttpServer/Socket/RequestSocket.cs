using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Net;



namespace VVVV.Webinterface.HttpServer
{

    /// <summary>
    /// Handles and analyse a HTTP Request.
    /// </summary>
    class   RequestSocket
    {


        private string mRequestType;
        private string mHttpVersion;
        private string mFilename;
        private string mFileLocation;
        private string FRequest;
        private SortedDictionary<string, string> mRequestHeadParameterList = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> mRequestBodyParameterList = new SortedDictionary<string, string>();
        
        private string mMessageHead = "";
        private string mMessageBody = "";
        private Dictionary<string,string> mArguments = new Dictionary<string,string>();
        private Response mResponse;
        private WebinterfaceSingelton mWebinterfaceSingelton = WebinterfaceSingelton.getInstance();

        private SortedList<string, byte[]> mHtmlPages;
        List<string> mFolderToServ;
        private SortedList<string, string> mPostMessages = new SortedList<string, string>();
        private SocketInformation FSocketInformation;

        # region public properties

        public string RequestType
        {
            get
            {
                return mRequestType;
            }
        }

        public string HttpVersion
        {
            get
            {
                return mHttpVersion;
            }
        }

        public string FileName
        {
            get
            {
                return mFilename;
            }
        }

        public string FileLocation
        {
            get
            {
                return mFileLocation;
            }
        }

        public SortedDictionary<string,string> ParameterList
        {
            get 
            {
                return mRequestHeadParameterList;
            }

        }

        public string MessageHead
        {
            get
            {
                return mMessageHead;
            }
        }

        public string MessageBody
        {
            get
            {
                return mMessageBody;
            }
        }

        public Response Response
        {
            get
            {
                return mResponse;
            }
        }

        # endregion public properties








        # region constructor

        public RequestSocket(List<string> pFolderToServ, SocketInformation SocketInformation, SortedList<string,string> pPostMessages)
        {



            this.mFolderToServ = pFolderToServ;
            this.FRequest = SocketInformation.Request.ToString();
            this.mPostMessages = pPostMessages;
            this.FSocketInformation = SocketInformation;

            int IndexOf2Newlines = FRequest.IndexOf("\r\n\r\n");

            mMessageHead = FRequest.Substring(0, IndexOf2Newlines);
            mMessageBody = FRequest.Substring(IndexOf2Newlines,FRequest.Length - IndexOf2Newlines); 
            mMessageBody = mMessageBody.TrimStart(new Char[] { '\n', '\r', '?' });
            string[] tHeadLines = mMessageHead.Split('\n');
            SplitHeadParameter(tHeadLines);
            SplitFirstLine(tHeadLines[0]);





            if (mRequestType == "GET" || mRequestType == "OPTIONS")
            {
                GetRequest();
            }

            else if (mRequestType == "POST")
            {

                PostRequest();
            }
            else
            {
                mResponse = new Response(mFilename, Encoding.UTF8.GetBytes("Error in Request Handling"), new HTTPStatusCode("").Code500);
            }    
        }

        #endregion constructor

        #region Analyse Http Head


        private void SplitHeadParameter(string[] pParameter)
        {

                int tLength = pParameter.Length;
                for (int i = 1; i < pParameter.Length; i++)
                {
                    string line = pParameter[i];
                    if (line.Contains(":"))
                    {
 
                        try
                        {
                        mRequestHeadParameterList.Add(line.Substring(0, line.IndexOf(":")), line.Substring(line.LastIndexOf(":") + 1));
                        //////Debug.WriteLine(line.Substring(0, line.IndexOf(":")) + ":" + line.Substring(line.LastIndexOf(":") + 1));
                        }catch(Exception ex)
                        {
                                    
                            Debug.WriteLine(" Error in Reading page Header " + Environment.NewLine + ex.Message);
                        }

                    }
                }
        }

        private void SplitFirstLine(string pFirstLine)
        {
            // RequestType
            string[] words = pFirstLine.Split(' ');


            mRequestType = words[0];
            //////Debug.WriteLine("RequestType: " + mRequestType);

            mHttpVersion = words[2].Substring(words[2].LastIndexOf('/') + 1);
            //////Debug.WriteLine("HttpVersion: " + mHttpVersion);

            if (words[1] == "/" && words[0] == "GET")
            {
                mFileLocation = words[1];
                mFilename = "index.html";
            }
            else
            {
                //FileName & Location
                string p01 = @"[?][\w\W]*$";
                mFileLocation = Regex.Replace(words[1], p01, "");
                mFilename = mFileLocation.Substring(mFileLocation.LastIndexOf('/') + 1);
            }

            //////Debug.WriteLine("mFilename: " +  mFilename);
            //////Debug.WriteLine("mFileLocation: " + mFileLocation);

            //GetProperties
            if(mRequestType =="GET" || mRequestType == "OPTIONS")
            {
                if (words[1].Contains("?"))
                {
                    string p02 = @"[\w\W]+[?]";
                    string getProperties = Regex.Replace(words[1], p02, "");
                    mMessageBody = getProperties;

                    string[] ParamterPairs = getProperties.Split('&');

                    foreach (string pPair in ParamterPairs)
                    {
                        string[] pGetPostParameters = pPair.Split('=');
                        if (pGetPostParameters.Length > 1)
                        {
                            mRequestBodyParameterList.Add(pGetPostParameters[0], pGetPostParameters[1]);
                        }
                    }
                }
                else
                {
                    return;
                }
            }
         }

        private string DetectedFileExtension(string pContenType)
        {
            string tReqeustedFileExtension = "unknown";

            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type");

            foreach (string keyName in key.GetSubKeyNames())
            {
                Microsoft.Win32.RegistryKey temp = key.OpenSubKey(keyName);

                if (pContenType.Equals(keyName))
                {
                    if (temp.GetValue("Extension") != null)
                    {
                       tReqeustedFileExtension = temp.GetValue("Extension").ToString();
                    }
                }
            }

            return tReqeustedFileExtension;
        }




        

        #endregion Analyse Http Head





        #region GET Request


        private void GetRequest()
        {
             mWebinterfaceSingelton.setResponseMessage(mMessageBody, mRequestType);
             mResponse = new Response(mFilename,new LoadSelectContent(mFilename, mFileLocation, mFolderToServ).ContentAsBytes, new HTTPStatusCode("").Code200);
             
        }


        #endregion GET Request



        #region POST Request


        private void PostRequest()
        {
            string tContentTypeHeader;
            mRequestHeadParameterList.TryGetValue("Content-Type", out tContentTypeHeader);

            string[] tValues = tContentTypeHeader.Split(';');

            string ContentType = tValues[0];
            ContentType = ContentType.Trim();

            string tReqeustedFileExtension = String.Empty;


            mWebinterfaceSingelton.setResponseMessage(mMessageBody, mRequestType);
            string tContentType = String.Empty;


            mRequestHeadParameterList.TryGetValue("Content-Type", out tContentType);
            mResponse = new Response(mFilename, tContentType, Encoding.UTF8.GetBytes("VVVV Received POST Request, but file not found"), new HTTPStatusCode("").Code404);
            string tRemoteIPAdresse = FSocketInformation.ClientSocket.RemoteEndPoint.ToString();
            tRemoteIPAdresse = tRemoteIPAdresse.Split(':')[0];

            string[] tVVVVParameter = mMessageBody.Split('&');

            switch (mFilename)
            {

                case "ToVVVV.xml":


                        foreach (string tValuePair in tVVVVParameter)
                        {
                            string[] tValue = tValuePair.Split('=');

                            if (tValue.Length > 1)
                            {
                                mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
                            }
                        }
                        mResponse = new Response(mFilename, ContentType, Encoding.UTF8.GetBytes("VVVV Received Post Request"), new HTTPStatusCode("").Code200);
                    break;




                case "MakeMeMaster.xml":


                    mWebinterfaceSingelton.SetMaster(tRemoteIPAdresse, tVVVVParameter[0].Split('=')[1]);

                    foreach (string tValuePair in tVVVVParameter)
                    {
                        string[] tValue = tValuePair.Split('=');

                        if (tValue.Length > 1)
                        {
                            mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
                        }
                    }

                    mResponse = new Response(mFilename, ContentType, Encoding.UTF8.GetBytes("You are Master: " + tRemoteIPAdresse), new HTTPStatusCode("").Code200);
                break;





                case "CheckIfSlave.xml":

                        string tResponse = mWebinterfaceSingelton.CheckIfSlave(tRemoteIPAdresse, tVVVVParameter[0].Split('=')[1]);

                        if (tResponse == "Master")
                        {
                            mResponse = new Response(mFilename, ContentType, Encoding.UTF8.GetBytes(tResponse), new HTTPStatusCode("").Code200);

                            foreach (string tValuePair in tVVVVParameter)
                            {
                                string[] tValue = tValuePair.Split('=');

                                if (tValue.Length > 1)
                                {
                                    mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
                                }
                            }
                        }
                        else
                        {
                            mResponse = new Response(mFilename, ContentType, Encoding.UTF8.GetBytes(tResponse), new HTTPStatusCode("").Code200);
                        }
                    break;





                case "polling.xml":
                    XmlDocument tMessage;
                    mWebinterfaceSingelton.getPollingMessage(out tMessage);

                    if (tMessage != null)
                    {
                        StringWriter sw = new StringWriter();
                        XmlTextWriter xw = new XmlTextWriter(sw);

                        // Save Xml Document to Text Writter.
                        tMessage.WriteTo(xw);
                        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

                        // Convert Xml Document To Byte Array.
                        byte[] docAsBytes = encoding.GetBytes(sw.ToString());

                        mResponse = new Response(mFilename, "text/xml", docAsBytes, new HTTPStatusCode("").Code200);
                    }
                    else
                    {
                        mResponse = new Response(mFilename, "text/xml", Encoding.UTF8.GetBytes("NoNewData"), new HTTPStatusCode("").Code200);
                    }
                break;



                case "uploadify.php":

                    mResponse = new Response(mFilename, "text/xml", Encoding.UTF8.GetBytes("1"), new HTTPStatusCode("").Code200);
                    break;




                default:
                        if (mPostMessages.ContainsKey(mFilename))
                        {
                            string PostResponse;
                            mPostMessages.TryGetValue(mFilename, out PostResponse);
                            mResponse = new Response(mFilename, "text/xml", Encoding.UTF8.GetBytes(PostResponse), new HTTPStatusCode("").Code200);
                        }
                        else
                        {
                            tReqeustedFileExtension = DetectedFileExtension(ContentType);
                            if (tReqeustedFileExtension == "unknown" && ContentType.Contains("javascript"))
                            {
                                tReqeustedFileExtension = ".js";
                            }
                        }
                    break;
            }


            //if (mFilename == "ToVVVV.xml")
            //{

            //    string tRemoteIPAdresse = mSocketInformation.ClientSocket.RemoteEndPoint.ToString();
            //    tRemoteIPAdresse = tRemoteIPAdresse.Split(':')[0];
            //    string[] tVVVVParameter = mMessageBody.Split('&');


            //    foreach (string tValuePair in tVVVVParameter)
            //    {
            //        string[] tValue = tValuePair.Split('=');
                    
            //        if (tValue.Length > 1)
            //        {
            //            mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
            //        }
            //    }
            //    mResponse = new Response(mFilename, tContentType, Encoding.UTF8.GetBytes("VVVV Received Post Request"), new HTTPStatusCode("").Code200);
            //}
            //else if (mFilename == "MakeMeMaster.xml")
            //{
            //    string tRemoteIPAdresse = mSocketInformation.ClientSocket.RemoteEndPoint.ToString();
            //    tRemoteIPAdresse = tRemoteIPAdresse.Split(':')[0];
            //    string[] tVVVVParameter = mMessageBody.Split('&');

            //    mWebinterfaceSingelton.SetMaster(tRemoteIPAdresse, tVVVVParameter[0].Split('=')[1]);

            //    foreach (string tValuePair in tVVVVParameter)
            //    {
            //        string[] tValue = tValuePair.Split('=');

            //        if (tValue.Length > 1)
            //        {
            //            mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
            //        }
            //    }

            //    mResponse = new Response(mFilename, tContentType, Encoding.UTF8.GetBytes("You are Master: " + tRemoteIPAdresse), new HTTPStatusCode("").Code200);
            //}
            //else if (mFilename == "CheckIfSlave.xml")
            //{
            //    string tRemoteIPAdresse = mSocketInformation.ClientSocket.RemoteEndPoint.ToString();
            //    tRemoteIPAdresse = tRemoteIPAdresse.Split(':')[0];
            //    string[] tVVVVParameter = mMessageBody.Split('&');

            //    string tResponse = mWebinterfaceSingelton.CheckIfSlave(tRemoteIPAdresse,tVVVVParameter[0].Split('=')[1]);

            //    if (tResponse == "Master")
            //    {
            //        mResponse = new Response(mFilename, tContentType, Encoding.UTF8.GetBytes(tResponse), new HTTPStatusCode("").Code200);

            //        foreach (string tValuePair in tVVVVParameter)
            //        {
            //            string[] tValue = tValuePair.Split('=');

            //            if (tValue.Length > 1)
            //            {
            //                mWebinterfaceSingelton.setNewBrowserDaten(tValue[0], tValue[1]);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        mResponse = new Response(mFilename, tContentType, Encoding.UTF8.GetBytes(tResponse), new HTTPStatusCode("").Code200);
            //    }

                
            //}
            //else if (mFilename == "polling.xml")
            //{
            //    XmlDocument tMessage;
            //    mWebinterfaceSingelton.getPollingMessage(out tMessage);

            //    if (tMessage != null)
            //    {
            //        StringWriter sw = new StringWriter();
            //        XmlTextWriter xw = new XmlTextWriter(sw);

            //        // Save Xml Document to Text Writter.
            //        tMessage.WriteTo(xw);
            //        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            //        // Convert Xml Document To Byte Array.
            //        byte[] docAsBytes = encoding.GetBytes(sw.ToString());

            //        mResponse = new Response(mFilename, "text/xml", docAsBytes, new HTTPStatusCode("").Code200);
            //    }
            //    else
            //    {
            //        mResponse = new Response(mFilename, "text/xml", Encoding.UTF8.GetBytes("NoNewData"), new HTTPStatusCode("").Code200);
            //    }


            //}
            //else if (mPostMessages.ContainsKey(mFilename))
            //{
            //    string PostResponse;
            //    mPostMessages.TryGetValue(mFilename, out PostResponse);
            //    mResponse = new Response(mFilename, "text/xml", Encoding.UTF8.GetBytes(PostResponse), new HTTPStatusCode("").Code200);
            //}
            //else
            //{
            //    tReqeustedFileExtension = DetectedFileExtension(ContentType);
            //    if (tReqeustedFileExtension == "unknown" && ContentType.Contains("javascript"))
            //    {
            //        tReqeustedFileExtension = ".js";
            //    }
            //}
        }

        #endregion POST Request


        private void SetReceivedData(string pContent)
        {

        }

    }
}
