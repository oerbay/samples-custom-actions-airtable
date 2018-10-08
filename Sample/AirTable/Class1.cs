using Newtonsoft.Json;
using Pagos.Designer.Interfaces.External.CustomHooks;
using Pagos.Designer.Interfaces.External.Messaging;
using Pagos.SpreadsheetWeb.Web.Api.Objects.Calculation;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace AirTable
{
    public class Request
    {
        public FieldsObject fields { get; set; }
    }
    /// <summary>
    /// Field names of table.
    /// </summary>
    public class FieldsObject
    {
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime RecordDate { get; set; }
    }

    /// <summary>
    /// Air table response object
    /// </summary>
    public class Response
    {
        public string id { get; set; }
        public FieldsObject fields { get; set; }
        public DateTime createdTime { get; set; }
    }

    public class Class1 : IAfterCalculation
    {
        private string applicationId = "your_airtable_application_id";
        private string tableName = "your_airtable_table_name";
        private string apiKey = "your_airtable_api_key";

        public Class1()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Air table uses oauth for service calls. This method will be our helper to access air table services.
        /// To see detailed information about end points and paramters please visit below link.
        /// https://airtable.com/api
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">air table api service url, it must be set to https://api.airtable.com/</param>
        /// <param name="controller">base id of your account.</param>
        /// <param name="action">Method name</param>
        /// <param name="data">data object </param>
        /// <param name="bearerToken">api key for your account.</param>
        /// <returns></returns>
        public T Post<T>(string url, string controller, string action, object data, string bearerToken = null)
        {
            var request = (HttpWebRequest)WebRequest.Create($"{url}/{controller}/{action}");

            request.Method = "POST";
            request.Timeout = 180000;

            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.Headers["Authorization"] = $"Bearer {bearerToken}";
            }

            var json = JsonConvert.SerializeObject(data,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var encoding = new UTF8Encoding();
            var content = encoding.GetBytes(json);

            request.ContentLength = content.Length;
            request.ContentType = @"application/json";

            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(content, 0, content.Length);
            }

            string responseText;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
            {
                responseText = reader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<T>(responseText);
        }

        public ActionableResponse AfterCalculation(CalculationRequest request, CalculationResponse response)
        {

            var name = request.Inputs.FirstOrDefault(x => x.Ref == "iName");
            var phone = request.Inputs.FirstOrDefault(x => x.Ref == "iPhone");
            var email = request.Inputs.FirstOrDefault(x => x.Ref == "iEmail");
            var userName = request.Inputs.FirstOrDefault(x => x.Ref == "iUserName");

            Request myRequest = new Request();
            myRequest.fields = new FieldsObject();
            myRequest.fields.Name = name.Value[0][0].Value;
            myRequest.fields.UserName = userName.Value[0][0].Value;
            myRequest.fields.Email = email.Value[0][0].Value;
            myRequest.fields.Phone = phone.Value[0][0].Value;
            myRequest.fields.RecordDate = DateTime.Now;

            try
            {
                var result = Post<Response>("https://api.airtable.com/v0", applicationId, tableName, myRequest, apiKey);
                response.Outputs.FirstOrDefault(x => x.Ref == "oResponse").Value[0][0].Value = result.id;
                return new ActionableResponse
                {
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return new ActionableResponse
                {
                    Success = false,
                    Messages = new System.Collections.Generic.List<string>() { ex.Message }
                };
            }
        }
    }
}