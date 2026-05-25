from __future__ import annotations

import argparse
import csv
import subprocess
from pathlib import Path

import imageio_ffmpeg
import numpy as np
import soundfile as sf
from scipy import signal


def run_ffmpeg_extract(input_path: Path, wav_path: Path, sample_rate: int) -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [
        ffmpeg,
        "-y",
        "-i",
        str(input_path),
        "-vn",
        "-ac",
        "1",
        "-ar",
        str(sample_rate),
        "-sample_fmt",
        "s16",
        str(wav_path),
    ]
    subprocess.run(cmd, check=True)


def zscore(values: np.ndarray) -> np.ndarray:
    return (values - np.mean(values)) / (np.std(values) + 1e-8)


def smooth(values: np.ndarray, frames: int) -> np.ndarray:
    frames = max(1, int(frames))
    kernel = np.ones(frames, dtype=np.float64) / frames
    return np.convolve(values, kernel, mode="same")


def contiguous_segments(mask: np.ndarray, hop: int, sample_rate: int) -> list[tuple[float, float]]:
    segments: list[tuple[float, float]] = []
    start: int | None = None
    for index, active in enumerate(mask):
        if active and start is None:
            start = index
        elif not active and start is not None:
            segments.append((start * hop / sample_rate, index * hop / sample_rate))
            start = None
    if start is not None:
        segments.append((start * hop / sample_rate, len(mask) * hop / sample_rate))
    return segments


def merge_short_gaps(segments: list[tuple[float, float]], gap: float = 0.18) -> list[tuple[float, float]]:
    if not segments:
        return []
    merged = [segments[0]]
    for start, end in segments[1:]:
        last_start, last_end = merged[-1]
        if start - last_end <= gap:
            merged[-1] = (last_start, end)
        else:
            merged.append((start, end))
    return merged


def isolate_knife(input_wav: Path, output_wav: Path, clips_wav: Path, segments_csv: Path) -> None:
    audio, sample_rate = sf.read(input_wav, dtype="float32")
    if audio.ndim > 1:
        audio = np.mean(audio, axis=1)

    audio = audio - np.mean(audio)
    peak = np.max(np.abs(audio)) + 1e-9
    audio = audio / peak

    sos_hp = signal.butter(6, 1500, btype="highpass", fs=sample_rate, output="sos")
    sos_lp = signal.butter(4, 13000, btype="lowpass", fs=sample_rate, output="sos")
    bright = signal.sosfiltfilt(sos_lp, signal.sosfiltfilt(sos_hp, audio))

    nperseg = 2048
    hop = 512
    freqs, times, spec = signal.stft(
        audio,
        fs=sample_rate,
        window="hann",
        nperseg=nperseg,
        noverlap=nperseg - hop,
        boundary=None,
    )
    power = np.abs(spec) ** 2

    high = power[(freqs >= 2500) & (freqs <= 12000)].sum(axis=0)
    mid = power[(freqs >= 700) & (freqs < 2500)].sum(axis=0)
    low = power[freqs < 700].sum(axis=0)
    high_log = np.log1p(high)
    onset = np.maximum(0, np.diff(high_log, prepend=high_log[0]))
    ratio = np.log1p(high / (mid + low + 1e-9))
    flux = np.sqrt(np.maximum(0, np.diff(power, axis=1, prepend=power[:, :1]))).sum(axis=0)

    score = zscore(high_log) + 1.25 * zscore(onset) + 0.85 * zscore(ratio) + 0.55 * zscore(flux)
    score = smooth(score, frames=max(1, round(0.035 * sample_rate / hop)))

    threshold = max(0.75, float(np.percentile(score, 82)))
    mask = score > threshold

    pad_frames = max(1, round(0.14 * sample_rate / hop))
    mask = np.convolve(mask.astype(float), np.ones(pad_frames * 2 + 1), mode="same") > 0

    # Keep the most metallic/transient-heavy moments if the adaptive mask grows too wide.
    active_ratio = float(np.mean(mask))
    if active_ratio > 0.45:
        threshold = float(np.percentile(score, 90))
        mask = score > threshold
        mask = np.convolve(mask.astype(float), np.ones(pad_frames * 2 + 1), mode="same") > 0

    frame_envelope = signal.savgol_filter(mask.astype(float), 17 if len(mask) >= 17 else max(3, len(mask) // 2 * 2 + 1), 2)
    frame_envelope = np.clip(frame_envelope, 0, 1)
    sample_positions = np.linspace(0, len(audio) - 1, len(frame_envelope))
    envelope = np.interp(np.arange(len(audio)), sample_positions, frame_envelope)
    envelope = signal.sosfiltfilt(signal.butter(2, 16, btype="lowpass", fs=sample_rate, output="sos"), envelope)
    envelope = np.clip(envelope, 0, 1)

    isolated = bright * envelope

    # A little transient restoration helps short blade swipes remain audible after gating.
    transient = bright * np.clip(envelope * 1.35, 0, 1)
    isolated = 0.72 * isolated + 0.28 * transient
    out_peak = np.max(np.abs(isolated)) + 1e-9
    isolated = isolated / out_peak * 0.92

    sf.write(output_wav, isolated.astype(np.float32), sample_rate)

    segments = merge_short_gaps(contiguous_segments(mask, hop, sample_rate))
    clips: list[np.ndarray] = []
    with segments_csv.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["start_seconds", "end_seconds", "duration_seconds"])
        for start, end in segments:
            if end - start >= 0.04:
                writer.writerow([f"{start:.3f}", f"{end:.3f}", f"{end - start:.3f}"])
                start_sample = max(0, int(start * sample_rate))
                end_sample = min(len(isolated), int(end * sample_rate))
                clip = isolated[start_sample:end_sample].copy()
                fade = min(len(clip) // 3, int(0.015 * sample_rate))
                if fade > 1:
                    ramp = np.linspace(0, 1, fade)
                    clip[:fade] *= ramp
                    clip[-fade:] *= ramp[::-1]
                clips.append(clip)

    if clips:
        silence = np.zeros(int(0.08 * sample_rate), dtype=np.float32)
        clipped = []
        for clip in clips:
            clipped.extend([clip.astype(np.float32), silence])
        sf.write(clips_wav, np.concatenate(clipped[:-1]), sample_rate)
    else:
        sf.write(clips_wav, np.zeros(1, dtype=np.float32), sample_rate)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_video", type=Path)
    parser.add_argument("output_dir", type=Path)
    parser.add_argument("--sample-rate", type=int, default=44100)
    args = parser.parse_args()

    args.output_dir.mkdir(parents=True, exist_ok=True)
    extracted = args.output_dir / "source_audio.wav"
    isolated = args.output_dir / "knife_sound_isolated.wav"
    clips = args.output_dir / "knife_sound_only_clips.wav"
    segments = args.output_dir / "knife_sound_segments.csv"

    run_ffmpeg_extract(args.input_video, extracted, args.sample_rate)
    isolate_knife(extracted, isolated, clips, segments)

    print(f"source_audio={extracted}")
    print(f"knife_sound_isolated={isolated}")
    print(f"knife_sound_only_clips={clips}")
    print(f"segments_csv={segments}")


if __name__ == "__main__":
    main()
