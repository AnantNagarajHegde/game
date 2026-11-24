"""Restore the symmetric kernel-3 padding in the pinned Kannada VITS ONNX export."""
from pathlib import Path
import hashlib
import sys
import onnx

folder=Path(sys.argv[1])
source=folder/"model.onnx"
with source.open("rb") as stream:
    digest=hashlib.file_digest(stream,"sha256").hexdigest()
if digest!="e0365f4af5056ceb112c2cd410287606c540393a146351026637ed4d388adf75":
    raise ValueError("Unexpected source model")
model=onnx.load(source)
fixed=0
for node in model.graph.node:
    if node.op_type=="Conv" and "/text_encoder/encoder/layers." in node.name and "/feed_forward/conv_" in node.name:
        attrs={a.name:a for a in node.attribute}
        if list(attrs["kernel_shape"].ints)!=[3] or list(attrs["pads"].ints)!=[1,0]:
            raise ValueError(node.name)
        attrs["pads"].ints[1]=1
        fixed+=1
if fixed!=12:raise ValueError(f"Unexpected layer count {fixed}")
onnx.checker.check_model(model)
onnx.save(model, folder/"kannada.onnx")
