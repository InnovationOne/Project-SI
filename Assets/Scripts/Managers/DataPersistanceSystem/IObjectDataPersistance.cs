// This script is for objects to implement saving and loading since they differ from crops or items
// where all attibutes are the same
using Unity.Collections;

public interface IObjectDataPersistence {
    public void LoadObject(FixedString4096Bytes data);
    public string SaveObject();

}
