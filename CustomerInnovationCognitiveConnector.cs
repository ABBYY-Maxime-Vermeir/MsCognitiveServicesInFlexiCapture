using System;
using ABBYY.FlexiCapture;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace MsCognitiveServicesInFlexiCapture
{
    public class CustomerInnovationCognitiveConnector
    {

        private string subscriptionKey;
        private string endpoint;
        static readonly string uriBase = "/vision/v3.0//read/analyze";


        // Add your Computer Vision subscription key and endpoint to your environment variables.
        public string SubscriptionKey { get => subscriptionKey; set => subscriptionKey = value; }
        public string Endpoint { get => endpoint; set => endpoint = value; }

        public CustomerInnovationCognitiveConnector(string msEndPoint, string msSubscriptionkey)
        {

            this.SubscriptionKey = msSubscriptionkey;
            this.Endpoint = msEndPoint;


        }


        public void ReadHandPrintFromBinary(byte[] image)
        {

            // Call the REST API method.
            Console.WriteLine("\nExtracting text...\n");
            ReadText(SubscriptionKey, Endpoint, image).Wait();

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        public async void ReadDocument(IDocument doc, IProcessingCallback callback)
        {

            // Call the REST API method.
            Console.WriteLine("\nExtracting text...\n");
            callback.ReportMessage("\nExtracting text...\n");

            string contentstring = await ReadText(SubscriptionKey, Endpoint, doc.SaveAsStream());

            JToken jsondata = JToken.Parse(contentstring);



        }

        public async void ReadHandPrint(IField field)
        {

            // Call the REST API method.
            Console.WriteLine("\nExtracting text...\n");

            string filepath = @"C:\temp\" + Guid.NewGuid().ToString("N");
            field.Regions[0].Picture.SaveAs(filepath);

            byte[] image = this.GetImageAsByteArray(filepath);

            string contentstring = await ReadText(SubscriptionKey, Endpoint, image);

            string finalValue = "";
            JToken jsondata = JToken.Parse(contentstring);

            //string OneCharacter = (string)theData.SelectToken("analyzeResult.readResults[0].lines[0].text");

            foreach (JToken character in jsondata.SelectToken("analyzeResult.readResults[0].lines"))
            {

                finalValue += (string)character.SelectToken("text");
                Console.Write((string)character.SelectToken("text"));

            }

            field.Text = finalValue;
        }


        public async void ReadHandPrint(IFieldRegion field, IValue value)
        {

            // Call the REST API method.
            Console.WriteLine("\nExtracting text...\n");

            string filepath = @"C:\temp\" + Guid.NewGuid().ToString("N");
            field.Picture.SaveAs(filepath);

            byte[] image = this.GetImageAsByteArray(filepath);

            string contentstring = await ReadText(SubscriptionKey, Endpoint, image);

            string finalValue = "";
            JToken jsondata = JToken.Parse(contentstring);

            //string OneCharacter = (string)theData.SelectToken("analyzeResult.readResults[0].lines[0].text");
            
            foreach (JToken character in jsondata.SelectToken("analyzeResult.readResults[0].lines"))
            {

                finalValue += (string)character.SelectToken("text");
                Console.Write((string)character.SelectToken("text"));

            }

            value.Text = finalValue;
        }

        static async Task<string> ReadText(string key, string ep, byte[] image)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", key);


                HttpResponseMessage response;

                // Two REST API methods are required to extract text.
                // One method to submit the image for processing, the other method
                // to retrieve the text found in the image.

                // operationLocation stores the URI of the second REST API method,
                // returned by the first REST API method.
                string operationLocation;

                // Adds the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(image))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // The first REST API method, Batch Read, starts
                    // the async process to analyze the written text in the image.
                    response = await client.PostAsync(ep + uriBase, content);
                }

                // The response header for the Batch Read method contains the URI
                // of the second method, Read Operation Result, which
                // returns the results of the process in the response body.
                // The Batch Read operation does not return anything in the response body.
                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    string errorString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    return errorString;
                }

                // If the first REST API method completes successfully, the second 
                // REST API method retrieves the text written in the image.
                //
                // Note: The response may not be immediately available. Text
                // recognition is an asynchronous operation that can take a variable
                // amount of time depending on the length of the text.
                // You may need to wait or retry this operation.
                //
                // This example checks once per second for ten seconds.
                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1);

                if (i == 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return contentString;
                }

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n\n{0}\n",
                    JToken.Parse(contentString).ToString());
                
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
                return e.Message;
            }

            return "";
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        public byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }


        //TODO
        //https://devblogs.microsoft.com/cse/2018/05/07/handwriting-detection-and-recognition-in-scanned-documents-using-azure-ml-package-computer-vision-azure-cognitive-services-ocr/
        //Replaced by MS Cognitive services prediction API
    }
}
