using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using System;

public static class MatchHistoryLogger
{
    // URL de tu base de datos Realtime
    private const string DATABASE_URL = "https://login1-78a38-default-rtdb.firebaseio.com/";

    public static async void SaveMatch(string winnerTeam, string loserTeam, string winnerName, string loserName, int winnerPoints, int loserPoints, List<string> mapsPlayed)
    {
        if (!FirebaseInitializer.IsReady)
        {
            Debug.LogError("❌ Firebase no está listo.");
            return;
        }

        DatabaseReference dbRef = FirebaseDatabase.GetInstance(DATABASE_URL).RootReference;

        var matchData = new Dictionary<string, object>
        {
            { "winner_team", winnerTeam },
            { "loser_team", loserTeam },
            { "winner_nickname", winnerName }, // Nuevo
            { "loser_nickname", loserName },   // Nuevo
            { "winner_points", winnerPoints }, // 20
            { "loser_points", loserPoints },   // 0
            { "date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "played_maps", string.Join(", ", mapsPlayed) }
        };

        try
        {
            DatabaseReference newMatchRef = dbRef.Child("match_history").Push();
            await newMatchRef.SetValueAsync(matchData);
            Debug.Log($"✅ Historial guardado con Nombres y Puntos.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error guardando en Realtime Database: {ex.Message}");
        }
    }
}