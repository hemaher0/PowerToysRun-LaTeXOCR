import sys
import os
import warnings
from PIL import ImageGrab
from pix2tex.cli import LatexOCR
from munch import Munch


warnings.filterwarnings("ignore", category=UserWarning)

def main():
    if len(sys.argv) < 2:
        print("Error: Model path argument not provided.", file=sys.stderr)
        return

    model_path = sys.argv[1]
    use_gpu_str = sys.argv[2]
    use_gpu_flag = use_gpu_str.lower() == 'true'
    use_cuda = use_gpu_flag and torch.cuda.is_available()
    
    checkpoint_path = os.path.join(model_path, 'checkpoints', 'weights.pth')

    if not os.path.exists(checkpoint_path):
        print("[No result] Model files not found. Please run the setup first by activating the plugin.", file=sys.stderr)
        return

    try:
        img = ImageGrab.grabclipboard()
        if img is None:
            print("[No result] Clipboard does not contain an image.", end="")
            return

        args = Munch({
            'config': os.path.join(model_path, 'settings', 'config.yaml'),
            'checkpoint': checkpoint_path,
            'tokenizer': os.path.join(model_path, 'tokenizer.json'),
            'no_cuda': not use_cuda, 
            'no_resize': False,
        })

        model = LatexOCR(args)
        latex_result = model(img)
        print(latex_result, end="")
        
    except Exception as e:
        print(f"Python Error: {e}", file=sys.stderr)

if __name__ == "__main__":
    main()
