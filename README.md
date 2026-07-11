![](.github/header.png)

A Unity Editor tool that lets you generate light cookies and simulate shadows... fresh out of the oven (i'm sorry) 

---

## Features

* **Per-light cookie baking**
* **Support for Spot, Directional, and Point lights**
* **Occluder list system** (each with its own settings)
  * Opacity
  * Dilate (grow)
  * Erode (shrink)
* **Multiple blur methods** (None, Gaussian, Kawase, Spiral)
* **Shadow opacity & cookie brightness controls**
* **Configurable bake camera**
* **Automatic naming** (name cookies after the light, or after a parent object N levels up)
* **Batch baking** (`GameObject > Light Cookies > Bake Cookies in Children`)
* **Auto-assign to light** (freshly baked cookie is assigned automatically)
* **Full Undo support**
* **Progress bar**
* **Probably some more**

---

## Frequently Asked Questions

* #### **What does this tool actually do?**
Basically, it creates a temporary camera at the light's position, rendering each occluder individually. It then composites the layers together by taking the minimum value across all different layers. After this is done and IF a type of blur was selected, it runs a blur pass over the result. The final texture gets saved, and assigned back to the light. Because each occluder renders separately before compositing, individual controls are also available! The actual concept; rendering each mesh as flat black geometry and then compositing the results, was so much easier than I would've expected.

* #### **Which render pipelines are supported?**
Built-In, Universal, and High Definition are all supported. The tool detects the active pipeline automatically via `RenderPipelineInfo` and adjusts camera setup accordingly.

* #### **How do I bake a cookie for a single light?**
Add a `LightCookieData` component to the same GameObject as your `Light`, set up your occluders in the list, and press the Bake button in the inspector.

* #### **What happens with Point lights?**
Point lights bake all six cubemap faces and save the result as a `.cubemap` asset instead of a `.png`.