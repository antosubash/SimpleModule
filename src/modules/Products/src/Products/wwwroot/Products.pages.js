import { jsxs as r, jsx as a } from "react/jsx-runtime";
function t({ products: s }) {
  return /* @__PURE__ */ r("div", { className: "max-w-4xl mx-auto p-8", children: [
    /* @__PURE__ */ a("h1", { className: "text-3xl font-bold mb-6", children: "Products" }),
    /* @__PURE__ */ a("ul", { className: "space-y-3", children: s.map((e) => /* @__PURE__ */ r(
      "li",
      {
        className: "flex justify-between items-center p-4 rounded-lg border border-gray-200 dark:border-gray-700",
        children: [
          /* @__PURE__ */ a("span", { className: "font-medium", children: e.name }),
          /* @__PURE__ */ r("span", { className: "text-gray-600 dark:text-gray-400", children: [
            "$",
            e.price.toFixed(2)
          ] })
        ]
      },
      e.id
    )) })
  ] });
}
const c = {
  "Products/Browse": t
};
export {
  c as pages
};
