using Unity.Collections;

public interface IObjectDataPersistence {
    public void LoadObject(FixedString4096Bytes data);
    public FixedString4096Bytes SaveObject();

}
