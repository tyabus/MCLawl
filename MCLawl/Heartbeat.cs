/*
	Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl) Licensed under the
	Educational Community License, Version 2.0 (the "License"); you may
	not use this file except in compliance with the License. You may
	obtain a copy of the License at
	
	http://www.osedu.org/licenses/ECL-2.0
	
	Unless required by applicable law or agreed to in writing,
	software distributed under the License is distributed on an "AS IS"
	BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
	or implied. See the License for the specific language governing
	permissions and limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Collections;

namespace MCLawl
{

    public static class Heartbeat
    {
        static string hash;
        public static string serverURL;
        static string staticVars;

        static HttpWebRequest request;
        static Random lawlBeatSeed = new Random(Process.GetCurrentProcess().Id);
        static StreamWriter beatlogger;

        static System.Timers.Timer heartbeatTimer = new System.Timers.Timer(500);
        static System.Timers.Timer lawlBeatTimer;

        public static void Init()
        {
            if(Server.logbeat)
            {
                if(!File.Exists("heartbeat.log"))
                {
                    File.Create("heartbeat.log").Close();
                }
            }
            lawlBeatTimer = new System.Timers.Timer(1000 + lawlBeatSeed.Next(0, 2500));
            staticVars = "port=" + Server.port +
                            "&max=" + Server.players +
                            "&name=" + UrlEncode(Server.name) +
                            "&public=" + Server.pub +
                            "&version=" + Server.version +
                            "&salt=" + Server.salt +
                            "&software=" + "MCLawl " + Server.Version;

            Thread backupThread = new Thread(new ThreadStart(delegate
            {
                heartbeatTimer.Elapsed += delegate
                {
                    heartbeatTimer.Interval = 55000;
                    try
                    {
                        Pump();
                    }
                    catch (Exception e) { Server.ErrorLog(e); }
                };
                heartbeatTimer.Start();

                lawlBeatTimer.Start();
            }));
            backupThread.Start();
        }

        public static bool Pump()
        {
            string postVars = staticVars;

            string url = "http://www.classicube.net/heartbeat.jsp";
            int totalTries = 0;
    retry:  try
            {
                totalTries++;
                // append additional information as needed
                        if (Server.logbeat)
                        {
                            beatlogger = new StreamWriter("heartbeat.log", true);
                        }
 
                request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                byte[] formData = Encoding.ASCII.GetBytes(postVars);
                request.ContentLength = formData.Length;
                request.Timeout = 15000;

   retryStream: try
                {
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(formData, 0, formData.Length);
                        beatlogger.WriteLine("Request sent at " + DateTime.Now.ToString());
                        requestStream.Close();
                    }
                }
                catch (WebException e)
                {
                    //Server.ErrorLog(e);
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        beatlogger.WriteLine("Timeout detected at " + DateTime.Now.ToString());
                        goto retryStream;
                        //throw new WebException("Failed during request.GetRequestStream()", e.InnerException, e.Status, e.Response);
                    }
                    else if (Server.logbeat)
                    {
                        beatlogger.WriteLine("Non-timeout exception detected: " + e.Message);
                        beatlogger.Write("Stack Trace: " + e.StackTrace);
                    }
                }

                //if (hash == null)
                //{
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        if (hash == null)
                        {
                            string line = responseReader.ReadLine();
                            if (Server.logbeat)
                            {
                                beatlogger.WriteLine("Response received at " + DateTime.Now.ToString());
                                beatlogger.WriteLine("Received: " + line);
                            }
                            hash = line.Substring(line.LastIndexOf('=') + 1);
                            serverURL = line;

                            Server.s.UpdateUrl(serverURL);
                            File.WriteAllText("text/externalurl.txt", serverURL);
                            Server.s.Log("URL found: " + serverURL);
                        }
                        else if (Server.logbeat)
                        {
                            beatlogger.WriteLine("Response received at " + DateTime.Now.ToString());
                        }
                    }
                }
                //}
                //Server.s.Log(string.Format("Heartbeat: {0}", type));
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    if (Server.logbeat)
                    {
                        beatlogger.WriteLine("Timeout detected at " + DateTime.Now.ToString());
                    }
                    Pump();
                }
            }
            catch
            {
                if (Server.logbeat)
                {
                    beatlogger.WriteLine("Heartbeat failure #" + totalTries + " at " + DateTime.Now.ToString());
                }
                if (totalTries < 3)
                {
                    goto retry;
                }
                if (Server.logbeat)
                {
                    beatlogger.WriteLine("Failed three times.  Stopping.");
                    beatlogger.Close();
                }
                return false;
            }
            finally
            {
                request.Abort();
            }
            if (beatlogger != null)
            {
                beatlogger.Close();
            }
            return true;
        }

        public static string UrlEncode(string input)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if ((input[i] >= '0' && input[i] <= '9') ||
                    (input[i] >= 'a' && input[i] <= 'z') ||
                    (input[i] >= 'A' && input[i] <= 'Z') ||
                    input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~')
                {
                    output.Append(input[i]);
                }
                else if (Array.IndexOf<char>(reservedChars, input[i]) != -1)
                {
                    output.Append('%').Append(((int)input[i]).ToString("X"));
                }
            }
            return output.ToString();
        }

        public static char[] reservedChars = { ' ', '!', '*', '\'', '(', ')', ';', ':', '@', '&',
                                                 '=', '+', '$', ',', '/', '?', '%', '#', '[', ']' };
    }
}
