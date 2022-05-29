// <copyright file="RawImageVideoPlayer.cs" company="Google LLC">
//
// Copyright 2018 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Helper class that plays a video on a RawImage texture.
/// </summary>
[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
public class RawImageVideoPlayer : MonoBehaviour
{
    /// <summary>
    /// The raw image where the video will be played.
    /// </summary>
    public RawImage rawImage;

    /// <summary>
    /// The video player component to be played.
    /// </summary>
    public VideoPlayer videoPlayer;

    private Texture rawImageTexture;

    /// <summary>
    /// The Unity Start() method.
    /// </summary>
    public void Start()
    {
        videoPlayer.enabled = false;
        rawImageTexture = rawImage.texture;
        videoPlayer.prepareCompleted += PrepareCompleted;
    }

    /// <summary>
    /// The Unity Update() method.
    /// </summary>
    public void Update()
    {
        if (ARSession.state != ARSessionState.SessionTracking || (ARSession.state == ARSessionState.None &&
            ARSession.notTrackingReason != NotTrackingReason.None))
        {
            videoPlayer.Stop();
            return;
        }

        if (rawImage.enabled && !videoPlayer.enabled)
        {
            videoPlayer.enabled = true;
            videoPlayer.Play();
        }
        else if (!rawImage.enabled && videoPlayer.enabled)
        {
            // Stop video playback to save power usage.
            videoPlayer.Stop();
            rawImage.texture = rawImageTexture;
            videoPlayer.enabled = false;
        }
    }

    private void PrepareCompleted(VideoPlayer player)
    {
        rawImage.texture = player.texture;
    }
}
