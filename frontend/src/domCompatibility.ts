const patchMarker = "__nestyStayDomCompatibilityPatched";

declare global {
  interface Node {
    [patchMarker]?: boolean;
  }
}

export function installDomCompatibilityGuards() {
  if (typeof window === "undefined" || typeof Node === "undefined") {
    return;
  }

  if (Node.prototype[patchMarker]) {
    return;
  }

  Object.defineProperty(Node.prototype, patchMarker, {
    configurable: false,
    enumerable: false,
    value: true,
  });

  const nativeRemoveChild = Node.prototype.removeChild;
  const nativeInsertBefore = Node.prototype.insertBefore;

  Node.prototype.removeChild = function patchedRemoveChild<T extends Node>(this: Node, child: T): T {
    if (child.parentNode === this) {
      return nativeRemoveChild.call(this, child) as T;
    }

    if (child.parentNode) {
      return nativeRemoveChild.call(child.parentNode, child) as T;
    }

    return child;
  };

  Node.prototype.insertBefore = function patchedInsertBefore<T extends Node>(
    this: Node,
    newNode: T,
    referenceNode: Node | null,
  ): T {
    if (referenceNode && referenceNode.parentNode !== this) {
      return this.appendChild(newNode) as T;
    }

    return nativeInsertBefore.call(this, newNode, referenceNode) as T;
  };
}
