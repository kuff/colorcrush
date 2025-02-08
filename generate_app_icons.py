#!/usr/bin/env python3
# Copyright (C) 2025 Peter Guld Leth

from PIL import Image, ImageDraw, ImageFilter, ImageChops
import os
import sys
import numpy as np
from collections import Counter

def color_distance(c1, c2):
    """Calculate Euclidean distance between two colors in RGB space"""
    return sum(abs(a - b) for a, b in zip(c1[:3], c2[:3]))

def get_median_color(img):
    """Get the median color from the non-transparent pixels"""
    img_array = np.array(img)
    alpha = img_array[:, :, 3]
    visible_pixels = img_array[alpha > 0]
    if len(visible_pixels) == 0:
        return None
    pixels = [tuple(pixel) for pixel in visible_pixels]
    return Counter(pixels).most_common(1)[0][0]

def hex_to_rgba(hex_color):
    """Convert hex color to RGBA tuple"""
    hex_color = hex_color.lstrip('#')
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4)) + (255,)

def replace_color_with_tolerance(img, target_color, replacement_color, tolerance=120):
    """Replace a color and similar colors within tolerance"""
    img_array = np.array(img)
    height, width = img_array.shape[:2]
    
    # Consider a pixel for replacement if it's similar to target color
    for y in range(height):
        for x in range(width):
            if img_array[y, x, 3] > 0:  # Only consider non-transparent pixels
                dist = color_distance(img_array[y, x], target_color)
                if dist <= tolerance:
                    # Very aggressive blend factor with cubic falloff
                    blend = (1.0 - (dist / tolerance)) ** 0.3  # Cube root for even slower falloff
                    # Blend between original and replacement color
                    blended_color = [
                        int(o * (1 - blend) + r * blend)
                        for o, r in zip(img_array[y, x], replacement_color)
                    ]
                    img_array[y, x] = blended_color
                elif dist <= tolerance * 2.0:  # Much wider extended range
                    # More aggressive edge blending
                    blend = 0.5 * (1.0 - (dist / (tolerance * 2.0)))
                    blended_color = [
                        int(o * (1 - blend) + r * blend)
                        for o, r in zip(img_array[y, x], replacement_color)
                    ]
                    img_array[y, x] = blended_color
    
    # Second pass to smooth any remaining edges
    result = img_array.copy()
    for y in range(1, height-1):
        for x in range(1, width-1):
            if img_array[y, x, 3] > 0:
                # Check if this pixel is at the edge of a replacement
                neighbors = [
                    img_array[y-1, x],
                    img_array[y+1, x],
                    img_array[y, x-1],
                    img_array[y, x+1]
                ]
                avg_color = np.mean(neighbors, axis=0)
                if color_distance(img_array[y, x], target_color) > tolerance:
                    # If neighbors are significantly different, blend with average
                    if any(color_distance(n, img_array[y, x]) > tolerance/2 for n in neighbors):
                        result[y, x] = [
                            int(0.7 * img_array[y, x][i] + 0.3 * avg_color[i])
                            for i in range(4)
                        ]
    
    return Image.fromarray(result)

def create_rounded_rectangle_mask(size, radius):
    """Create a mask with rounded corners with improved anti-aliasing"""
    scale = 4
    large_size = (size[0] * scale, size[1] * scale)
    large_radius = radius * scale
    
    mask = Image.new('L', large_size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle([(0, 0), (large_size[0]-1, large_size[1]-1)], large_radius, fill=255)
    
    mask = mask.filter(ImageFilter.GaussianBlur(radius=scale/2))
    mask = mask.resize(size, Image.Resampling.LANCZOS)
    
    return mask

def find_largest_inscribed_square(alpha_channel):
    """Find the largest square that fits inside the non-transparent area"""
    height, width = alpha_channel.shape
    center_y, center_x = height // 2, width // 2
    
    max_size = min(width, height)
    for size in range(max_size, 0, -1):
        half_size = size // 2
        x1 = center_x - half_size
        x2 = center_x + half_size
        y1 = center_y - half_size
        y2 = center_y + half_size
        
        if x1 < 0 or x2 >= width or y1 < 0 or y2 >= height:
            continue
            
        square = alpha_channel[y1:y2, x1:x2]
        if np.all(square > 0):
            return (x1, y1, x2, y2)
    
    return None

def process_image(input_path, output_dir, size=(1024, 1024)):
    """Process the image to create both square and rounded versions"""
    try:
        with Image.open(input_path) as img:
            img = img.convert('RGBA')
            
            # Get median color
            median_color = get_median_color(img)
            if median_color is None:
                raise ValueError("Could not determine median color")
            
            # Convert target color to replace
            target_color = hex_to_rgba('E7C930')
            
            # Replace color with tolerance and blending
            img = replace_color_with_tolerance(img, target_color, median_color)
            
            # Get alpha channel as numpy array
            alpha = np.array(img.split()[3])
            
            # Find largest inscribed square
            square_bounds = find_largest_inscribed_square(alpha)
            if square_bounds is None:
                raise ValueError("Could not find valid square region in image")
                
            # Create the mask with iOS corner radius (22.5%)
            square_size = square_bounds[2] - square_bounds[0]
            mask = create_rounded_rectangle_mask((square_size, square_size), square_size * 0.225)
            
            # Crop the image to the square bounds
            cropped = img.crop(square_bounds)
            
            # Apply the mask with improved blending
            img_channels = list(cropped.split())
            img_channels[3] = ImageChops.multiply(img_channels[3], mask)
            masked_img = Image.merge('RGBA', img_channels)
            
            # Create final images at 1024x1024
            square_img = Image.new('RGBA', size, (0, 0, 0, 0))
            rounded_img = Image.new('RGBA', size, (0, 0, 0, 0))
            
            # Resize the images with high-quality resampling
            original_resized = cropped.resize(size, Image.Resampling.LANCZOS)
            masked_resized = masked_img.resize(size, Image.Resampling.LANCZOS)
            
            # Save square version
            square_path = os.path.join(output_dir, 'AppIcon_Square.png')
            original_resized.save(square_path, 'PNG', quality=95)
            print(f"Square icon saved to: {square_path}")
            
            # Save rounded version
            rounded_path = os.path.join(output_dir, 'AppIcon_Rounded.png')
            masked_resized.save(rounded_path, 'PNG', quality=95)
            print(f"Rounded icon saved to: {rounded_path}")
            
    except Exception as e:
        print(f"Error processing image: {str(e)}")
        sys.exit(1)

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    resources_dir = os.path.join(script_dir, 'Assets', 'Resources', 'Colorcrush', 'Emoji')
    output_dir = os.path.join(script_dir, 'Assets')  # Changed to save in Assets folder
    
    if len(sys.argv) > 1:
        # Get the emoji filename from arguments
        emoji_name = sys.argv[1]
        # Search in both Happy and Sad directories
        possible_paths = [
            os.path.join(resources_dir, 'Happy', emoji_name),
            os.path.join(resources_dir, 'Sad', emoji_name)
        ]
        
        # Try to find the emoji in either directory
        input_path = None
        for path in possible_paths:
            if os.path.exists(path):
                input_path = path
                break
            # Try adding .png if not already present
            if os.path.exists(path + '.png'):
                input_path = path + '.png'
                break
        
        if input_path is None:
            print(f"Error: Could not find emoji '{emoji_name}' in the Resources directory")
            print("Please provide the emoji filename (e.g. reshot-icon-cat-BZV4EQRNPJ)")
            sys.exit(1)
    else:
        print("Please provide the emoji filename as an argument")
        print("Example: python generate_app_icons.py reshot-icon-cat-BZV4EQRNPJ")
        sys.exit(1)
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    process_image(input_path, output_dir)

if __name__ == "__main__":
    main() 