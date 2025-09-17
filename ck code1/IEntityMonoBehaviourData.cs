using UnityEngine;

public interface IEntityMonoBehaviourData
{
	GameObject GameObject { get; }

	Transform Transform { get; }

	ObjectInfo ObjectInfo { get; }
}
