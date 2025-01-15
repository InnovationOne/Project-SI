public interface ICloudSaveProvider {
    void Upload(string profileId, string data);
    string Download(string profileId);
    bool HasCloudSave(string profileId);
}