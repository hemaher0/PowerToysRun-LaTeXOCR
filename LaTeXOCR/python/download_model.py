import os
import sys
import warnings
from pix2tex.model.checkpoints.get_latest_checkpoint import download_checkpoints
from pix2tex.utils import in_model_path

warnings.filterwarnings("ignore", category=UserWarning)

@in_model_path()
def download_model():
    expected_files = [
        'checkpoints/weights.pth',
        'checkpoints/image_resizer.pth'
    ]
    if all(os.path.exists(file) for file in expected_files):
        print("Model files already exist. No download needed.")
        return

    print("Downloading model files... This may take a while.", flush=True)
    try:
        download_checkpoints()
        
        if all(os.path.exists(file) for file in expected_files):
            print("Model download complete.")
        else:
            print("Download process finished, but some model files are still missing.", file=sys.stderr)
    except Exception as e:
        print(f"An error occurred during model download: {e}", file=sys.stderr)

if __name__ == "__main__":
    download_model()
