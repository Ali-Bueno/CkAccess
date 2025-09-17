using UnityEngine.Scripting;

[RequireImplementors]
public interface IPugJsonSerializer
{
	int RuntimeTypeIndex { get; }

	ulong SerializedTypeHash { get; }

	string SerializeToJson(object data);

	object DeserializeFromJson(string json);
}
