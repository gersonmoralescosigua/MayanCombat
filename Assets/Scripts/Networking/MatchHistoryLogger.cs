using UnityEngine;
using Firebase.Database; // Cambiado de Firestore a Database
using System.Collections.Generic;
using System;

public static class MatchHistoryLogger
{
    // Ya no son necesarios los atributos de Firestore.
    // Mantenemos la estructura limpia.
    public struct MatchResultData
    {
        public string winner { get; set; }
        public string loser { get; set; }
        public string score { get; set; }
        public string date { get; set; }
        public string played_maps { get; set; }
    }

    public static async void SaveMatch(string winnerTeam, string loserTeam, int winnerScore, int loserScore, List<string> mapsPlayed)
    {
        if (!FirebaseInitializer.IsReady)
        {
            Debug.LogError("❌ Firebase no está listo.");
            return;
        }

        // Obtenemos la referencia a la base de datos (Root)
        DatabaseReference dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Preparamos los datos.
        // Nota: Para Realtime Database en Unity, lo más seguro y robusto es pasar los datos
        // como un Diccionario <string, object> para evitar problemas de serialización con propiedades {get; set;}
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
            // 1. Accedemos al nodo "match_history" (se crea si no existe)
            // 2. Usamos .Push() para generar un ID único automáticamente (igual que el ID del documento en Firestore)
            DatabaseReference newMatchRef = dbRef.Child("match_history").Push();

            // 3. Guardamos los datos
            await newMatchRef.SetValueAsync(matchData);

            Debug.Log($"✅ Historial guardado en Realtime Database ID: {newMatchRef.Key}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error guardando en Firebase: {ex.Message}");
        }
    }
}