// Pure Firebase Unity Project
// Author : Soundpiercer
// soundpiercer@gmail.com
// Firebase Unity SDK v6.3.0

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SocialPlatforms;

using GooglePlayGames;
using GooglePlayGames.BasicApi;

using Firebase.Auth;

public class FirebaseManager : MonoBehaviour
{
    public Text firebaseAuthStatusText;

    private const string FIREBASE_AUTH_STATUS_NONE = "status : none";

    // Comment from Soundpiercer:
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

    #region Firebase Auth
    public void GooglePlayLogin()
    {
        StartCoroutine(GooglePlayLoginEnumerator());
    }

    private IEnumerator GooglePlayLoginEnumerator()
    {
        yield return StartCoroutine(GPGSLoginEnumerator());
        yield return StartCoroutine(FirebaseAuthLoginEnumerator());
        GetFirebaseUser();
    }

    string _aa = "";

    private IEnumerator GPGSLoginEnumerator()
    {
        bool hasFinished = false;

        // Initialize GPGS -- Copied from FirebaseAuth Unity SDK Documentation
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
        .RequestServerAuthCode(false /* Don't force refresh */)
        .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();

        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                Debug.Log("[Firebase Manager] >>>>>> GPGS Login Success");
            }
            else
            {
                Debug.Log("[Firebase Manager] >>>>>> GPGS Login Failed");
            }

            _aa = PlayGamesPlatform.Instance.GetServerAuthCode();

            hasFinished = true;
        });

        yield return new WaitUntil(() => hasFinished);
    }

    private IEnumerator FirebaseAuthLoginEnumerator()
    {
        bool hasFinished = false;

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        Credential credential =
            PlayGamesAuthProvider.GetCredential(_aa);
        auth.SignInWithCredentialAsync(credential).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);

            hasFinished = true;
        });

        yield return new WaitUntil(() => hasFinished);
    }

    private void GetFirebaseUser()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            firebaseAuthStatusText.text = "<color=yellow>Firebase Auth Success\nID : " + user.DisplayName + " ID : " + user.UserId + "</color>";
        }
        else
        {
            firebaseAuthStatusText.text = "Firebase Auth Failed";
        }
    }

    public void Logout()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        auth.SignOut();

        if (Social.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.SignOut();
        }

        firebaseAuthStatusText.text = FIREBASE_AUTH_STATUS_NONE;
    }
    #endregion

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

    public void QuitApp()
    {
        Application.Quit();
    }
}