/**
 * Parts of the source code in this file has been translated/ported from jsonld.js library by Digital Bazaar (BSD 3-Clause license)
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JsonLd.Normalization
{
    public static class JsonLdHandler
    {
        /// <summary>
        /// JSON-LD context for issuer dependent properties
        /// </summary>
        public static JObject IssuerDependentContext = JObject.Parse("{ '@vocab': 'https://www.w3.org/ns/credentials/issuer-dependent#' }");

        /// <summary>
        /// Implements URDNA2015 normalization/canonicalization algorithm based on jsonld.js v5.2.0 implementation, 
        /// the same as Normalize
        /// </summary>
        /// <param name="json">serialized json document</param>
        /// <param name="options">options to be used during the document expansion process</param>
        /// <returns>normalized n-quads document as string</returns>
        public static async Task<string> Canonize(string json, ExpandOptions options = null, Func<object, JToken> expansionMap = null)
        {
            return await Normalize(json, options, expansionMap);
        }

        /// <summary>
        /// Implements URDNA2015 normalization/canonicalization algorithm based on jsonld.js v5.2.0 implementation,
        /// the same as Canonize
        /// </summary>
        /// <param name="json">serialized json document</param>
        /// <param name="options">options to be used during the document expansion process</param>
        /// <returns>normalized n-quads document as string</returns>
        public static async Task<string> Normalize(string json, ExpandOptions options = null, Func<object, JToken> expansionMap = null)
        {
            var dataset = await ToRDF(json, options, expansionMap);
            return URDNA2015.Normalize(dataset);
        }

        private static async Task<List<Quad>> ToRDF(string json, ExpandOptions options = null, Func<object, JToken> expansionMap = null)
        {
            var expanded = await Expand(json, options, expansionMap);
            return expanded.ToRDF();
        }

        private static async Task<JToken> Expand(string json, ExpandOptions options = null, Func<object, JToken> expansionMap = null)
        {
            var activeCtx = new ExpandContext();
            JToken token;
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                token = JToken.Load(reader);

            var contexts = (token["@context"] as JArray);
            if (contexts != null)
            {
                if (!contexts.Contains(IssuerDependentContext, new JTokenEqualityComparer()))
                {
                    contexts.Add(IssuerDependentContext);
                }
            }

            var objects = new List<JObject>();
            if (token.Type == JTokenType.Array)
                objects.AddRange(token.ToArray().Where(t => t.Type == JTokenType.Object).Cast<JObject>());
            if (token.Type == JTokenType.Object)
                objects.Add((JObject)token);
            var result = new JArray();
            foreach (var doc in objects)
            {
                options ??= new();
                var expanded = await Expansion.Expand(activeCtx, doc, null, options, expansionMap: expansionMap );

                // optimize away @graph with no other properties
                if (expanded?.Type == JTokenType.Object)
                {
                    var expandedObj = (JObject)expanded;
                    if (expandedObj.TryGetValue("@graph", out var graphProp) && expandedObj.Properties().Count() == 1)
                        expanded = graphProp;
                }

                if (expanded != null)
                    result.Add(expanded);
            }

            return result;
        }
    }
}
