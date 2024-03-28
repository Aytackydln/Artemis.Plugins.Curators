using System.Text.Json.Serialization;

namespace Artemis.Plugins.Curators.Curations;

[JsonSerializable(typeof(Curation))]
public partial class JsonSourceContext : JsonSerializerContext;