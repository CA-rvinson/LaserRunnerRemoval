# Script to build and install the ml_model package. Long term we might want to host a CSC_ml_models on PyPi and install it like any other requirement
cd ml_model 
python setup.py bdist_wheel
#This is currently tied to the current version
pip install dist/ml_model-0.1.0-py3-none-any.whl