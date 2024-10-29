using System.Text;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace JsonLd.Normalization.Tests
{
    public class Tests
    {
        string clrJson = "";

        [SetUp]
        public void Setup()
        {
            using var stream = new StreamReader(typeof(Tests).Assembly.GetManifestResourceStream("JsonLd.Normalization.Tests.Files.clr2_0.json"));
            clrJson = stream.ReadToEnd();
        }

        public object GetProperty(Object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return propertyInfo.GetValue(obj);
        }

        public bool HasProperty(Object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return propertyInfo != null;
        }

        [Test]
        public async Task Test1()
        {
            JObject document;
            using (var reader = new JsonTextReader(new StringReader(clrJson)) { DateParseHandling = DateParseHandling.None })
                document = JObject.Load(reader);
            var json = document.ToString();
            var canonicalizedDocument = await JsonLd.Normalization.JsonLdHandler.Canonize(json, new JsonLd.Normalization.ExpandOptions { Base = "c14n", IsFrame = false, KeepFreeFloatingNodes = true });
            Assert.IsTrue(canonicalizedDocument.Contains("RevocationList"));
        }
    }
}