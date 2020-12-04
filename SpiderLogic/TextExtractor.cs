using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace TextExtractor
{
    /// <summary>
    /// Logic to parse website and get urls
    /// </summary>
    public class UrlExtractor
    {
        /// <summary>
        /// Returns the urls in specified site address
        /// </summary>
        /// <param name="baseUrl">Base Url</param>
        /// <param name="recursive">If true, parses recursively through all links</param>
        /// <returns></returns>
        public static IList<string> GetUrls(string url, bool recursive)
        {
            string absoluteBaseUrl = url;
            if (!absoluteBaseUrl.EndsWith("/"))
                absoluteBaseUrl += "/";

            return GetUrls(url, absoluteBaseUrl, recursive);
        }

        /// <summary>
        /// Returns the urls in specified site address
        /// </summary>
        /// <param name="url">Base Url</param>
        /// <param name="recursive">If true, parses recursively through all links</param>
        /// <returns></returns>
        public static IList<string> GetUrls(string url, string baseUrl, bool recursive)
        {
            if (recursive)
            {
                _urls.Clear();
                RecursivelyGenerateUrls(url, baseUrl);

                return _urls;
            }
            else
                return InternalGetUrls(url, baseUrl);
        }

        /// <summary>
        /// Internal method that recursively generates urls
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="absoluteBaseUrl"></param>
        private static void RecursivelyGenerateUrls(string baseUrl, string absoluteBaseUrl)
        {
            var urls = InternalGetUrls(baseUrl, absoluteBaseUrl);

            foreach (string url in urls)
            {
                if (!_urls.Contains(url))
                {
                    _urls.Add(url);

                    string newAbsoluteBaseUrl = GetBasePath(url);
                    RecursivelyGenerateUrls(url, newAbsoluteBaseUrl);
                }
            }
        }

        private static string GetBasePath(string baseUrl)
        {
            if (baseUrl.EndsWith("/"))
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

            if (baseUrl.Contains("/"))
            {
                int index = baseUrl.LastIndexOf("/");
                string basePath = baseUrl.Substring(0, index + 1);

                if (!basePath.EndsWith("/"))
                    basePath += "/";

                return basePath;
            }
            return baseUrl;
        }

        private static IList<string> _urls = new List<string>();

        private static IList<string> InternalGetUrls(string baseUrl, string absoluteBaseUrl)
        {
            IList<string> list = new List<string>();

            Uri uri = null;
            if (!Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out uri))
                return list;

            // Get the http content
            string siteContent = GetHttpResponse(baseUrl);

            var allUrls = GetAllUrls(siteContent);

            foreach (string uriString in allUrls)
            {
                uri = null;
                if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri))
                {
                    if (uri.IsAbsoluteUri)
                    {
                        if (uri.OriginalString.StartsWith(absoluteBaseUrl)) // If different domain / javascript: urls needed exclude this check
                        {
                            list.Add(uriString);
                        }
                    }
                    else
                    {
                        string newUri = GetAbsoluteUri(uri, absoluteBaseUrl, uriString);
                        if (!string.IsNullOrEmpty(newUri))
                            list.Add(newUri);
                    }
                }
                else
                {
                    if (!uriString.StartsWith(absoluteBaseUrl))
                    {
                        string newUri = GetAbsoluteUri(uri, absoluteBaseUrl, uriString);
                        if (!string.IsNullOrEmpty(newUri))
                            list.Add(newUri);
                    }
                }
            }

            return list;
        }

        private static string GetAbsoluteUri(Uri uri, string basePath, string uriString)
        {
            if (!string.IsNullOrEmpty(uriString))
                if (uriString.Contains(":"))
                    if (!uriString.Contains("http:"))
                        return string.Empty;

            basePath = GetResolvedBasePath(basePath, uriString);
            uriString = uriString.Replace("../", string.Empty);

            uri = null;
            string newUriString = basePath;
            if (!newUriString.EndsWith("/"))
                newUriString += "/";

            newUriString += uriString;

            newUriString = newUriString.Replace("//", "/");

            if (Uri.TryCreate(newUriString, UriKind.RelativeOrAbsolute, out uri))
                return newUriString;

            return string.Empty;
        }

        private static string GetResolvedBasePath(string basePath, string uriString)
        {
            int count = GetCountOf("../", uriString);
            for (int i = 1; i <= count; i++)
            {
                basePath = GetBasePath(basePath);
            }

            return basePath;
        }

        private static int GetCountOf(string pattern, string str)
        {
            int count = 0;
            int index = -1;

            while (true)
            {
                index = str.IndexOf(pattern, index + 1);
                if (index == -1)
                    break;

                count++;
            }

            return count;
        }

        /// <summary>
        /// Returns all urls in string content
        /// [Includes javascrip:, mailto:, other domains too]
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string[] GetAllUrls(string str)
        {
            string pattern = @"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>";

            System.Text.RegularExpressions.MatchCollection matches
                = System.Text.RegularExpressions.Regex.Matches(str, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            string[] matchList = new string[matches.Count];

            int c = 0;

            foreach (System.Text.RegularExpressions.Match match in matches)
                matchList[c++] = match.Groups["url"].Value;

            return matchList;
        }

        /// <summary>
        /// Returns the response content as string for given url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetHttpResponse(string url)
        {
            try
            {
                ASCIIEncoding encoding = new ASCIIEncoding();

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "GET";

                HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();

                return GetResponseContent(response);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return String.Empty;
        }

        #region "Exception Handling"

        public delegate void OnExceptionDelegate(Exception ex);

        /// <summary>
        /// OnException delegate can be used to handle the exceptions inside this class
        /// </summary>
        public static OnExceptionDelegate OnException;

        private static void HandleException(Exception ex)
        {
            if (OnException != null)
                OnException(ex);
        }

        #endregion

        /// <summary>
        /// Returns the string content of HttpWebResponse
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static string GetResponseContent(HttpWebResponse response)
        {
            if (response == null)
                return String.Empty;

            StringBuilder builder = new StringBuilder();
            Stream stream = response.GetResponseStream();

            StreamReader streamReader = new StreamReader(stream);

            int data = 0;
            do
            {
                data = streamReader.Read();
                if (data > -1)
                    builder.Append((char)data);
            }
            while (data > -1);

            streamReader.Close();

            return builder.ToString();
        }
    }
    public class EmailExtractor
    {
        //public method called from your application 
        public static IList<string> RetrieveEmails(string webPage)
        {
            return ExtractAllEmails(RetrieveContent(webPage));
        }

        //get the content of the web page passed in 
        private static string RetrieveContent(string webPage)
        {
            HttpWebResponse response = null;//used to get response 
            StreamReader respStream = null;//used to read response into string 
            try
            {
                //create a request object using the url passed in 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webPage);
                request.Timeout = 10000;

                //go get a response from the page 
                response = (HttpWebResponse)request.GetResponse();

                //create a streamreader object from the response 
                respStream = new StreamReader(response.GetResponseStream());

                //get the contents of the page as a string and return it 
                return respStream.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                response.Close();
                respStream.Close();
            }
        }

        //using a regular expression, find all of the href or urls in the content of the page 
        private static IList<string> ExtractAllEmails(string content)
        {
            //regular expression 
            string pattern = "";
            //pattern = @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@" + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\." + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|" + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";

            //pattern = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
            //    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";

            //pattern = @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}";

            //pattern = @"\b[A-Z0-9._-]+@[A-Z0-9][A-Z0-9.-]{0,61}[A-Z0-9]\.[A-Z.]{2,6}\b";

            pattern = @"^[a-z0-9._-]+@(?:yahoo\.com|yahoo\.com\.vn|gmail\.com|hotmail\.com)$";

            //Set up regex object 
            Regex reg = new Regex(pattern, RegexOptions.IgnoreCase);

            Match match;

            List<string> results = new List<string>();
            
            //Loop through matches 
            for (match = reg.Match(content); match.Success; match = match.NextMatch())
            {
                if (!(results.Contains(match.Value)))
                    results.Add(match.Value);
            }

            return results;
        }        
    }
}
