using System.Collections.Generic;
using System.Threading.Tasks;
using static rCubedToolkit.Http;

namespace rCubedToolkit
{
    class GameVersions
    {
        public static string versions_url = "http://www.flashflashrevolution.com/game/r3/r3-gameVersions.php";
        public static string manifest_url = "http://www.flashflashrevolution.com/game/r3/r3-gameManifest.php?id=";
        public static List<JSONGameVersion> VersionList;

        public static async Task GetVersions()
        {
            var resp = await HttpClient.GetAsync(versions_url);
            var body = await resp.Content.ReadAsStringAsync();
            VersionList = JsonSerializer.Deserialize<List<JSONGameVersion>>(body);
        }
    }

    public class JSONGameVersion
    {
        public int id;
        public string edition;
        public string version;
        public int version_hash;
        public int api_version;
        public string url;
        public string swf_hash;
        public int arch;
    }

    public class JSONManifestList
    {
        public string path;
        public string hash;
    }
}

   
