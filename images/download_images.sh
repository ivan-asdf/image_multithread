#!/bin/zsh

# Number of iterations (folders)
iterations=45

# Number of images per folder
images_per_folder=100

# Loop to create folders and download images
for i in {25..$iterations}
do
  # Create a folder named with the iteration number (1, 2, 3, etc.)
  folder_name="$i"
  mkdir "$folder_name"

  # Download 10 images in this folder
  for j in {1..$images_per_folder}
  do
    wget -O "$folder_name/img_$j.jpg" "https://picsum.photos/3840/2160"
    
    # Sleep for a random time between 1 to 3 seconds
    sleep 1 # This will sleep for 1 to 3 seconds
  done
done

