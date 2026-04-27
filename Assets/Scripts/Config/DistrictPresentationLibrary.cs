using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/District Presentation Library", fileName = "DistrictPresentationLibrary")]
public sealed class DistrictPresentationLibrary : ScriptableObject
{
    [SerializeField] private DistrictPresentationProfile[] profiles;

    public DistrictPresentationProfile[] Profiles => profiles;

    public DistrictPresentationProfile GetProfile(float distance)
    {
        RunDistrictCatalog.DistrictInfo district = RunDistrictCatalog.Resolve(distance);
        return GetProfileByIndex(district.Index);
    }

    public DistrictPresentationProfile GetProfileByIndex(int districtIndex)
    {
        if (profiles == null || profiles.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            DistrictPresentationProfile profile = profiles[i];
            if (profile != null && profile.DistrictIndex == districtIndex)
            {
                return profile;
            }
        }

        return profiles[0];
    }
}
