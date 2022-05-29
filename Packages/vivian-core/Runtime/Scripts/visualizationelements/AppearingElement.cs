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
     * This class represents an AppearingObject element to be appear or disappear.
     */
    public class AppearingElement : VisualizationElement<AppearingObjectSpec, bool>
    {
        // the represented game object
        private GameObject representedGameObject;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(AppearingObjectSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);
            this.representedGameObject = representedObject;

            if (this.representedGameObject == null)
            {
                throw new ArgumentException("represented object with name " + spec.Name +
                                            " does not exist");
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            this.representedGameObject.SetActive(value);

            this.Value = value;
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                this.representedGameObject.SetActive(true);
                this.Value = true;
            }
            else
            {
                this.representedGameObject.SetActive(false);
                this.Value = false;
            }
        }
    }
}
