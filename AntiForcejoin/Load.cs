using HarmonyLib;
using MelonLoader;
using Photon.Realtime;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using VRC.Core;

namespace AntiForcejoin
{
    public class Load : MelonMod
    {
        public override void OnApplicationStart()
        {
            InitPatch();
            Console.WriteLine("[AntiForcejoin] Loaded (https://github.com/Umbra999)");
        }
        private static readonly HarmonyLib.Harmony Instance = new HarmonyLib.Harmony("Graveyard");
        private static HarmonyMethod GetPatch(string name)
        {
            return new HarmonyMethod(typeof(Load).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static void CreatePatch(MethodInfo TargetMethod, HarmonyMethod Before = null, HarmonyMethod After = null)
        {
            try
            {
                Instance.Patch(TargetMethod, Before, After);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to Patch {TargetMethod.Name} \n{e}");
            }
        }

        public static unsafe void InitPatch()
        {
            CreatePatch(typeof(LoadBalancingPeer).GetMethod(nameof(LoadBalancingPeer.Method_Public_Virtual_New_Boolean_EnterRoomParams_2)), GetPatch(nameof(HandleInstanceFlow)));
            CreatePatch(typeof(BestHTTP.HTTPManager).GetMethods().Where(m => m.Name == "SendRequest" && m.GetParameters().Length == 1).First(), GetPatch(nameof(HeaderPatch)));
        }

        // If you dirty skids gonna steal it give me atleast credits :*

        // Telling the Photonserver to use a hidden instance type
        private static void HandleInstanceFlow(ref EnterRoomParams __0)
        {
            Logger.Log(__0.field_Public_String_0);
            if (__0.field_Public_String_0.Contains("~private") && __0.field_Public_String_0.Contains("~nonce") && __0.field_Public_String_0.Contains(APIUser.CurrentUser.id) && !__0.field_Public_String_0.Contains("~strict") && !__0.field_Public_String_0.Contains("~canRequestInvite"))
            {
                __0.field_Public_String_0 += "~strict";
                Console.WriteLine("[AntiForcejoin] Forced instance to be non Forcejoinable");
                MelonCoroutines.Start(WaitForInstance(__0.field_Public_String_0));
            }
        }

        // To fix API Invites
        private static IEnumerator WaitForInstance(string ID)
        {
            while (RoomManager.field_Internal_Static_ApiWorldInstance_0 == null) yield return null;
            RoomManager.field_Internal_Static_ApiWorldInstance_0.instanceId = ID.Split(':')[1];
        }

        // To get a special Roomtoken for the Hidden instance
        private static void HeaderPatch(BestHTTP.HTTPRequest request)
        {
            try
            {
                if (request.Uri.ToString().StartsWith("https://api.vrchat.cloud/api/1/instances/") && request.Uri.ToString().EndsWith("/join?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26&organization=vrchat") && request.Uri.ToString().Contains(APIUser.CurrentUser.id) && request.Uri.ToString().Contains("~private") && !request.Uri.ToString().Contains("~strict") && request.Uri.ToString().Contains("~nonce") && !request.Uri.ToString().Contains("~canRequestInvite"))
                {
                    string[] AdjustedURL = request.Uri.ToString().Split('/');
                    string NewID = AdjustedURL[6] + "~strict";
                    request.Uri = new Il2CppSystem.Uri(request.Uri.ToString().Replace(AdjustedURL[6], NewID));
                    Console.WriteLine("[AntiForcejoin] Got Token for non forcejoinable Instance");
                }
            }
            catch {  }
        }
    }
}
