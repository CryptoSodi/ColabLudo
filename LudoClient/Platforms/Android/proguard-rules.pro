-keep class org.jni_zero.** { *; }
-keep class org.webrtc.** { *; }

# Keep JNI bridge methods required by libjingle_peerconnection_so.so
-keep class org.jni_zero.JniInit {
    public static java.lang.Object[] init();
}
