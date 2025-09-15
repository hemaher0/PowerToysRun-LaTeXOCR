# TODO: Development and Feature Implementation

## Add Math Delimiter Feature

- **Functionality Definition**: Implement a feature that automatically adds math delimiters like inline formulas (`$...$`) or display formulas (`$$...$$`) to the LaTeX code when it's copied to the clipboard.
- **Implementation Details**:
  - Provide an option in the plugin settings to allow users to select their preferred delimiter (e.g., `None`, `$`, `$$`, `\(`, `\[`).

## Loading Message

- **Functionality Definition**: Display a loading message to the user to indicate progress while the LaTeX OCR model is processing the captured image.
- **Implementation Details**:
  - Show a message like "Processing..." or "Converting to LaTeX..." when the `ptr` command is run.
  - Update the message to "Complete!" or "Copied to clipboard" upon successful conversion.
  - Provide messages, such as "Error: Could not convert", in case of network errors or model processing failures.

## Add GPU Support

- **Functionality Definition**: Significantly improve conversion speed by offloading OCR model computations from the CPU to the GPU.
- **Implementation Details**:
  - Add logic to the installation script to check for a user's system GPU and download the GPU-supported packages.
  - Add a setting in the plugin that allows users to enable or disable GPU support manually.

## Implement Conversion History

- **Functionality Definition**: Create a feature that saves a history of all converted LaTeX formulas, allowing users to view, search, and reuse past conversions.
- **Implementation Details:
  - Add a command (e.g., `latex history`) to PowerToys Run to display a list of recent conversions.

## Write Detailed README.md

- Skip

## Create Logo

- Skip
