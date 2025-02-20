using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/CutsceneInfo")]
public class CutsceneInfo : ScriptableObject {
    [Tooltip("A general description of the cutscene.")]
    public string CutsceneDescription;

    [Tooltip("A unique identifier for this cutscene.")]
    public string CutsceneId;

    [Tooltip("List of segments that make up this cutscene.")]
    public List<CutsceneSegmentContainer> Segments;
}