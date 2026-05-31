package org.webrtc;

/**
 * Compatibility wrapper for newer WebRTC native builds that expect
 * org.webrtc.Environment to exist at runtime.
 */
public final class Environment implements AutoCloseable {
    private final long webrtcEnv;

    public static final class Builder {
        private String fieldTrials;

        public Builder setFieldTrials(String fieldTrials) {
            this.fieldTrials = fieldTrials;
            return this;
        }

        public Environment build() {
            return new Environment(this.fieldTrials);
        }
    }

    public static Builder builder() {
        return new Builder();
    }

    private Environment(String fieldTrials) {
        this.webrtcEnv = nativeCreate(fieldTrials);
    }

    public long ref() {
        return webrtcEnv;
    }

    public long getCurrentTimeNanos() {
        return nativeCurrentTimeNanos(webrtcEnv);
    }

    @Override
    public void close() {
        nativeFree(webrtcEnv);
    }

    private static native long nativeCreate(String fieldTrials);
    private static native long nativeCurrentTimeNanos(long webrtcEnv);
    private static native void nativeFree(long webrtcEnv);
}
