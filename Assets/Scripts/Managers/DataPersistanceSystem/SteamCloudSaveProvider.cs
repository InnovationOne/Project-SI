using Steamworks;
using System.Text;
using UnityEngine;

public class SteamCloudSaveProvider : ICloudSaveProvider {
    public void Upload(string profileId, string data) {
        // Use Steamworks to write to Steam Cloud
        byte[] fileData = Encoding.UTF8.GetBytes(data);
        bool success = SteamRemoteStorage.FileWrite(profileId, fileData, fileData.Length);
        if (!success) {
            Debug.LogError("Failed to upload save data to Steam Cloud for profile: " + profileId);
        } else {
            Debug.Log("Uploaded save data to Steam Cloud for profile: " + profileId);
        }
    }

    public string Download(string profileId) {
        if (!SteamRemoteStorage.FileExists(profileId)) {
            Debug.LogWarning("No Steam Cloud save found for profile: " + profileId);
            return null;
        }

        int fileSize = SteamRemoteStorage.GetFileSize(profileId);
        byte[] buffer = new byte[fileSize];
        int bytesRead = SteamRemoteStorage.FileRead(profileId, buffer, fileSize);
        if (bytesRead != fileSize) {
            Debug.LogError("Failed to read the entire file from Steam Cloud for profile: " + profileId);
            return null;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    public bool HasCloudSave(string profileId) {
        return SteamRemoteStorage.FileExists(profileId);
    }
}

