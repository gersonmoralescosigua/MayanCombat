using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;

public static class MatchHistoryLogger
{
    public struct MatchResultData
    {
        public string winner { get; set; }
        public string loser { get; set; }
        public string score { get; set; }
        public string date { get; set; }
        public string played_maps { get; set; }
    }

    // URL de tu base de datos Realtime
    private const string DATABASE_URL = "https://login1-78a38-default-rtdb.firebaseio.com/";

    public static async void SaveMatch(string winnerTeam, string loserTeam, int winnerScore, int loserScore, List<string> mapsPlayed)
    {
        if (!FirebaseInitializer.IsReady)
        {
            Debug.LogError("❌ Firebase no está listo.");
            return;
        }

        // --- CAMBIO CLAVE AQUÍ ---
        // En lugar de DefaultInstance, usamos GetInstance con tu URL específica.
        // Esto soluciona el problema si tu JSON es viejo y no trae la URL.
        DatabaseReference dbRef = FirebaseDatabase.GetInstance(DATABASE_URL).RootReference;

        var matchData = new Dictionary<string, object>
        {
            { "winner", winnerTeam },
            { "loser", loserTeam },
            { "score", $"{winnerScore}-{loserScore}" },
            { "date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "played_maps", string.Join(", ", mapsPlayed) }
        };

        try
        {
            DatabaseReference newMatchRef = dbRef.Child("match_history").Push();
            await newMatchRef.SetValueAsync(matchData);

            Debug.Log($"✅ Historial guardado en Realtime Database ID: {newMatchRef.Key}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error guardando en Realtime Database: {ex.Message}");
        }
    }
}