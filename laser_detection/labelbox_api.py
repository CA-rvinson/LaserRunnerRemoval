""" File: labelbox_api.py

Description: Functions for using the Labelbox API and creating yolo model labels from
Labelbox exports. 
"""

from glob import glob
import os
import labelbox as lb
import ndjson
from PIL import Image
from dotenv import load_dotenv

load_dotenv()
client = lb.Client(os.getenv("LABELBOX_API_KEY"))
dataset_name = "laser_detection"
project_name = "Laser Detection"
project = client.get_projects(where=lb.Project.name == project_name).get_one()
class_map = {"laser": 0}

data_dir = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "data_store/laser_detection",
)
img_dir = os.path.join(data_dir, "raw")
label_dir = os.path.join(data_dir, "raw_labels")


def import_images(dataset_name):
    dataset = client.get_datasets(where=lb.Dataset.name == dataset_name).get_one()
    if not dataset:
        dataset = client.create_dataset(name=dataset_name)
    global_keys = []
    img_paths = glob(os.path.join(img_dir, "*.jpg"))
    for img_path in img_paths:
        _, img_name = os.path.split(img_path)
        dataset.create_data_row({"row_data": img_path, "global_key": img_name})
        global_keys.append(img_name)


def create_yolo_labels_from_export_ndjson(filepath):
    """Given a labelbox export, create yolo model label files"""
    with open(filepath, "r") as f:
        rows = ndjson.load(f)

    for row in rows:
        image_filename = row["data_row"]["global_key"]
        label = row["projects"][project.uid]["labels"][0]
        annotations = label["annotations"]["objects"]
        annotation = next(
            (annotation for annotation in annotations if annotation["name"] == "laser"),
            None,
        )

        media_attributes = row["media_attributes"]
        height = media_attributes["height"]
        width = media_attributes["width"]

        yolo_label_name = os.path.splitext(image_filename)[0] + ".txt"
        yolo_label_path = os.path.join(label_dir, yolo_label_name)

        with open(yolo_label_path, "w") as yolo_label_file:
            if annotation is not None:
                point = annotation["point"]
                class_id = class_map[annotation["name"]]
                # Keypoint format: (class id, center_x, center_y, width, height, keypoint1_x, keypoint1_y, keypoint1_visibility, ..., keypointn_x, keypointn_y, keypointn_visibility)
                # visibility: 0 = not labeled, 1 = labeled but not visible, and 2 = labeled and visible
                yolo_label_file.write(
                    f"{class_id} {point['x'] / width} {point['y'] / height} 0 0 {point['x'] / width} {point['y'] / height} 2\n"
                )
