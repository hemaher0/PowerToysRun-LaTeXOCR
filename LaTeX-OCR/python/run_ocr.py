import sys
import warnings
from PIL import ImageGrab
from pix2tex.cli import LatexOCR

warnings.filterwarnings("ignore", category=UserWarning)

def main():
    try:
        img = ImageGrab.grabclipboard()
        if img is None:
            print("[No result] Clipboard has no image.", end="")
            return
        model = LatexOCR()
        latex_result = model(img)
        print(latex_result, end="")
    except Exception as e:
        print(f"Python Error: {e}", file=sys.stderr)

if __name__ == "__main__":
    main()
