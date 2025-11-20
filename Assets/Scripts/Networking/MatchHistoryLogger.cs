using UnityEngine;
using Firebase.Firestore; // Ahora esto dejará de dar error rojo
using System.Collections.Generic;
using System;

public static class MatchHistoryLogger
{
    // Esta etiqueta [FirestoreData] ahora será reconocida gracias al paquete que acabas de instalar
    [FirestoreData]
    public struct MatchResultData
    {
        [FirestoreProperty] public string winner { get; set; }
        [FirestoreProperty] public string loser { get; set; }
        [FirestoreProperty] public string score { get; set; }
        [FirestoreProperty] public string date { get; set; }
        [FirestoreProperty] public string played_maps { get; set; }
    }

    public static async void SaveMatch(string winnerTeam, string loserTeam, int winnerScore, int loserScore, List<string> mapsPlayed)
    {
        if (!FirebaseInitializer.IsReady)
        {
            Debug.LogError("❌ Firebase no está listo.");
            return;
        }

        var db = FirebaseFirestore.DefaultInstance;
        
        var matchData = new MatchResultData
        {
            winner = winnerTeam,
            loser = loserTeam,
            score = $"{winnerScore}-{loserScore}",
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            played_maps = string.Join(", ", mapsPlayed)
        };

        try
        {
            // Esto creará automáticamente la colección "match_history" si no existe
            DocumentReference docRef = await db.Collection("match_history").AddAsync(matchData);
            Debug.Log($"✅ Historial guardado en Firebase ID: {docRef.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error guardando en Firebase: {ex.Message}");
        }
    }
}