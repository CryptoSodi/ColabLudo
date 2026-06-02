-keep class org.jni_zero.** { *; }
-keep class org.webrtc.** { *; }
-keep class org.webrtc.WebRtcClassLoader { *; }
-keep class org.webrtc.NativeLibrary { *; }
-keep class org.webrtc.PeerConnectionFactory { *; }
-keep class org.webrtc.Environment { *; }
-keep class org.webrtc.NativeLibrary$DefaultLoader { *; }
-keep class org.webrtc.NativeLibraryLoader { *; }
-keep class org.jni_zero.JniInit { *; }
-keepnames class org.webrtc.** { *; }
-keepnames class org.jni_zero.** { *; }

# Keep JNI bridge methods required by libjingle_peerconnection_so.so
-keep class org.jni_zero.JniInit {
    public static java.lang.Object[] init();
}

# Keep AndroidX app component factory used at startup
-keep class androidx.core.app.CoreComponentFactory { *; }
-keep class androidx.core.** { *; }
-keep class androidx.appcompat.** { *; }
-keep class androidx.lifecycle.** { *; }
-keep class androidx.fragment.** { *; }

# Keep MAUI/Mono generated bridge classes frequently resolved reflectively
-keep class crc64**.** { *; }
-keep class crc65**.** { *; }
-keep class mono.** { *; }
-keep class microsoft.maui.** { *; }

# Google Sign-In uses this internal activity via explicit component launch.
# R8 must not strip/rename or make it non-instantiable.
-keep class com.google.android.gms.auth.api.signin.internal.SignInHubActivity { *; }
-keep class com.google.android.gms.auth.api.signin.internal.** { *; }
-keep class com.google.android.gms.auth.api.signin.** { *; }

# Avoid warnings from WebRTC/jni_zero internals when shrinking
-dontwarn org.webrtc.**
-dontwarn org.jni_zero.**
-dontwarn androidx.**
