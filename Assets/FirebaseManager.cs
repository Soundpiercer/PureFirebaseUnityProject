using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    // >>>>>> Comment from Soundpiercer
    // Firebase Init Operation is Async Task, so we're going to handle as Unity Coroutine.
    // If we don't, FirebaseApp will make Exception such as:
    // "InvalidOperationException: Don't call Firebase functions before CheckDependencies has finished"
    private void Start()
    {
        StartCoroutine(InitEnumerator());
    }

    private IEnumerator InitEnumerator()
    {
        yield return StartCoroutine(FirebaseAppInitEnumerator());
        yield return StartCoroutine(FirebaseCloudMessagingInitEnumerator());
    }

    private IEnumerator FirebaseAppInitEnumerator()
    {
        bool hasFinished = false;

        // Initialize Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                // Crashlytics will use the DefaultInstance, as well;
                // this ensures that Crashlytics is initialized.
                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here for indicating that your project is ready to use Firebase.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }

            hasFinished = true;
        });

        yield return new WaitUntil(() => hasFinished);
    }

    #region Firebase Cloud Messaging (FCM)
    private IEnumerator FirebaseCloudMessagingInitEnumerator()
    {
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;

        yield return null;
    }

    public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        UnityEngine.Debug.Log("Received Registration Token: " + token.Token);
    }

    public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        UnityEngine.Debug.Log("Received a new message from: " + e.Message.From);
    }
    #endregion
}