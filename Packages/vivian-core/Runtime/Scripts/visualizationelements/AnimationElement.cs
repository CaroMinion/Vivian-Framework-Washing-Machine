// Copyright 2019 Patrick Harms
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents an animation element to be played or stopped.
     */
    public class AnimationElement : VisualizationElement<AnimationSpec, bool>
    {
        // the animation to be played
        private Animation RepresentedAnimation;
        
        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(AnimationSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.RepresentedAnimation = representedObject.GetComponent<Animation>();

            if (this.RepresentedAnimation == null)
            {
                throw new ArgumentException("represented object with name " + representedObject.name +
                                            " does not have an animation");
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                this.RepresentedAnimation.Play();
            }
            else
            {
                this.RepresentedAnimation.Stop();
            }

            this.Value = value;
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                this.RepresentedAnimation.Play();
                this.Value = true;
            }
            else
            {
                this.RepresentedAnimation.Stop();
                this.Value = false;
            }
        }
    }
}
