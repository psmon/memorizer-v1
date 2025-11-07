
We recently completed the ASKBOT multi-modal feature as described in prompt/kr/29-AskBot-Multimodal.md.
Based on this, we will implement the following partial upgrade.

## Additional Features
- Currently, ASKBOT supports multi-modal image analysis, but when sharing conversations, images are not saved and cannot be viewed.
  - When images are uploaded, they will be saved locally, and when shared, the images can be loaded and displayed.
    - ASKBOT already uses markdown by default, so images will be displayed in markdown image format.
  - The image storage path can be configured in the application settings.
    - The default image storage path is set to the "AskBotImages" folder.


## Local Testing Method
- Resolve build errors only.
- After fixing build errors, testing will be done manually, and modifications will be made based on feedback.
