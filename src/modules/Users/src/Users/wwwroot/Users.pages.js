import { jsxs as t, jsx as e } from "react/jsx-runtime";
import { router as l } from "@inertiajs/react";
import { useState as b } from "react";
function x({ users: r, search: i, page: a, totalPages: s, totalCount: m }) {
  const [o, g] = b(i);
  function d(n) {
    n.preventDefault(), l.get("/admin/users", { search: o, page: 1 }, { preserveState: !0 });
  }
  function c(n) {
    l.get("/admin/users", { search: o, page: n }, { preserveState: !0 });
  }
  return /* @__PURE__ */ t("div", { className: "max-w-6xl mx-auto p-8", children: [
    /* @__PURE__ */ t("div", { className: "flex justify-between items-center mb-6", children: [
      /* @__PURE__ */ e("h1", { className: "text-3xl font-bold", children: "Users" }),
      /* @__PURE__ */ t("span", { className: "text-gray-500", children: [
        m,
        " total"
      ] })
    ] }),
    /* @__PURE__ */ t("form", { onSubmit: d, className: "mb-6 flex gap-2", children: [
      /* @__PURE__ */ e(
        "input",
        {
          type: "text",
          value: o,
          onChange: (n) => g(n.target.value),
          placeholder: "Search by name or email...",
          className: "flex-1 px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
        }
      ),
      /* @__PURE__ */ e(
        "button",
        {
          type: "submit",
          className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
          children: "Search"
        }
      )
    ] }),
    /* @__PURE__ */ e("div", { className: "overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700", children: /* @__PURE__ */ t("table", { className: "w-full text-left", children: [
      /* @__PURE__ */ e("thead", { className: "bg-gray-50 dark:bg-gray-800", children: /* @__PURE__ */ t("tr", { children: [
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Name" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Email" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Roles" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Status" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Created" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400" })
      ] }) }),
      /* @__PURE__ */ e("tbody", { className: "divide-y divide-gray-200 dark:divide-gray-700", children: r.map((n) => /* @__PURE__ */ t("tr", { className: "hover:bg-gray-50 dark:hover:bg-gray-800/50", children: [
        /* @__PURE__ */ e("td", { className: "px-4 py-3 font-medium", children: n.displayName || "—" }),
        /* @__PURE__ */ t("td", { className: "px-4 py-3 text-gray-600 dark:text-gray-400", children: [
          n.email,
          !n.emailConfirmed && /* @__PURE__ */ e("span", { className: "ml-2 text-xs text-amber-600 dark:text-amber-400", children: "unverified" })
        ] }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3", children: /* @__PURE__ */ e("div", { className: "flex gap-1 flex-wrap", children: n.roles.map((u) => /* @__PURE__ */ e(
          "span",
          {
            className: "px-2 py-0.5 text-xs rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300",
            children: u
          },
          u
        )) }) }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3", children: n.isLockedOut ? /* @__PURE__ */ e("span", { className: "text-red-600 dark:text-red-400 text-sm", children: "Locked" }) : /* @__PURE__ */ e("span", { className: "text-green-600 dark:text-green-400 text-sm", children: "Active" }) }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3 text-sm text-gray-500", children: new Date(n.createdAt).toLocaleDateString() }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3", children: /* @__PURE__ */ e(
          "button",
          {
            onClick: () => l.get(`/admin/users/${n.id}/edit`),
            className: "text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm",
            children: "Edit"
          }
        ) })
      ] }, n.id)) })
    ] }) }),
    s > 1 && /* @__PURE__ */ t("div", { className: "flex justify-center gap-2 mt-6", children: [
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => c(a - 1),
          disabled: a <= 1,
          className: "px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50",
          children: "Previous"
        }
      ),
      /* @__PURE__ */ t("span", { className: "px-3 py-1 text-gray-600 dark:text-gray-400", children: [
        "Page ",
        a,
        " of ",
        s
      ] }),
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => c(a + 1),
          disabled: a >= s,
          className: "px-3 py-1 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-50",
          children: "Next"
        }
      )
    ] })
  ] });
}
function h({ user: r, userRoles: i, allRoles: a }) {
  function s(d) {
    d.preventDefault();
    const c = new FormData(d.currentTarget);
    l.post(`/admin/users/${r.id}`, c);
  }
  function m(d) {
    d.preventDefault();
    const c = new FormData(d.currentTarget);
    l.post(`/admin/users/${r.id}/roles`, c);
  }
  function o() {
    l.post(`/admin/users/${r.id}/lock`);
  }
  function g() {
    l.post(`/admin/users/${r.id}/unlock`);
  }
  return /* @__PURE__ */ t("div", { className: "max-w-3xl mx-auto p-8", children: [
    /* @__PURE__ */ t("div", { className: "flex items-center gap-4 mb-6", children: [
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => l.get("/admin/users"),
          className: "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300",
          children: "← Back"
        }
      ),
      /* @__PURE__ */ e("h1", { className: "text-3xl font-bold", children: "Edit User" })
    ] }),
    /* @__PURE__ */ t("div", { className: "mb-6 text-sm text-gray-500 dark:text-gray-400", children: [
      /* @__PURE__ */ t("span", { children: [
        "Created: ",
        new Date(r.createdAt).toLocaleString()
      ] }),
      r.lastLoginAt && /* @__PURE__ */ t("span", { className: "ml-4", children: [
        "Last login: ",
        new Date(r.lastLoginAt).toLocaleString()
      ] })
    ] }),
    /* @__PURE__ */ t("form", { onSubmit: s, className: "mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: [
      /* @__PURE__ */ e("h2", { className: "text-lg font-semibold mb-4", children: "Details" }),
      /* @__PURE__ */ t("div", { className: "space-y-4", children: [
        /* @__PURE__ */ t("div", { children: [
          /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Display Name" }),
          /* @__PURE__ */ e(
            "input",
            {
              type: "text",
              name: "displayName",
              defaultValue: r.displayName,
              className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            }
          )
        ] }),
        /* @__PURE__ */ t("div", { children: [
          /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Email" }),
          /* @__PURE__ */ e(
            "input",
            {
              type: "email",
              name: "email",
              defaultValue: r.email,
              className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
            }
          )
        ] }),
        /* @__PURE__ */ t("div", { className: "flex items-center gap-2", children: [
          /* @__PURE__ */ e(
            "input",
            {
              type: "checkbox",
              name: "emailConfirmed",
              id: "emailConfirmed",
              defaultChecked: r.emailConfirmed,
              className: "rounded border-gray-300"
            }
          ),
          /* @__PURE__ */ e("label", { htmlFor: "emailConfirmed", className: "text-sm", children: "Email confirmed" })
        ] }),
        /* @__PURE__ */ e(
          "button",
          {
            type: "submit",
            className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
            children: "Save Details"
          }
        )
      ] })
    ] }),
    /* @__PURE__ */ t("form", { onSubmit: m, className: "mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: [
      /* @__PURE__ */ e("h2", { className: "text-lg font-semibold mb-4", children: "Roles" }),
      /* @__PURE__ */ t("div", { className: "space-y-2 mb-4", children: [
        a.map((d) => /* @__PURE__ */ t("div", { className: "flex items-center gap-2", children: [
          /* @__PURE__ */ e(
            "input",
            {
              type: "checkbox",
              name: "roles",
              value: d.name ?? "",
              id: `role-${d.id}`,
              defaultChecked: i.includes(d.name ?? ""),
              className: "rounded border-gray-300"
            }
          ),
          /* @__PURE__ */ t("label", { htmlFor: `role-${d.id}`, className: "text-sm", children: [
            d.name,
            d.description && /* @__PURE__ */ t("span", { className: "text-gray-500 ml-1", children: [
              "— ",
              d.description
            ] })
          ] })
        ] }, d.id)),
        a.length === 0 && /* @__PURE__ */ e("p", { className: "text-sm text-gray-500", children: "No roles defined." })
      ] }),
      /* @__PURE__ */ e(
        "button",
        {
          type: "submit",
          className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
          children: "Save Roles"
        }
      )
    ] }),
    /* @__PURE__ */ t("div", { className: "p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: [
      /* @__PURE__ */ e("h2", { className: "text-lg font-semibold mb-4", children: "Account Status" }),
      r.isLockedOut ? /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("p", { className: "text-sm text-red-600 dark:text-red-400 mb-3", children: "This account is locked." }),
        /* @__PURE__ */ e(
          "button",
          {
            onClick: g,
            className: "px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700",
            children: "Unlock Account"
          }
        )
      ] }) : /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("p", { className: "text-sm text-green-600 dark:text-green-400 mb-3", children: "This account is active." }),
        /* @__PURE__ */ e(
          "button",
          {
            onClick: o,
            className: "px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700",
            children: "Lock Account"
          }
        )
      ] })
    ] })
  ] });
}
function p({ roles: r }) {
  function i(a, s) {
    confirm(`Delete role "${s}"?`) && l.delete(`/admin/roles/${a}`, {
      onError: () => alert("Cannot delete role with assigned users.")
    });
  }
  return /* @__PURE__ */ t("div", { className: "max-w-4xl mx-auto p-8", children: [
    /* @__PURE__ */ t("div", { className: "flex justify-between items-center mb-6", children: [
      /* @__PURE__ */ e("h1", { className: "text-3xl font-bold", children: "Roles" }),
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => l.get("/admin/roles/create"),
          className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
          children: "Create Role"
        }
      )
    ] }),
    /* @__PURE__ */ e("div", { className: "overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700", children: /* @__PURE__ */ t("table", { className: "w-full text-left", children: [
      /* @__PURE__ */ e("thead", { className: "bg-gray-50 dark:bg-gray-800", children: /* @__PURE__ */ t("tr", { children: [
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Name" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Description" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Users" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400", children: "Created" }),
        /* @__PURE__ */ e("th", { className: "px-4 py-3 text-sm font-medium text-gray-500 dark:text-gray-400" })
      ] }) }),
      /* @__PURE__ */ e("tbody", { className: "divide-y divide-gray-200 dark:divide-gray-700", children: r.map((a) => /* @__PURE__ */ t("tr", { className: "hover:bg-gray-50 dark:hover:bg-gray-800/50", children: [
        /* @__PURE__ */ e("td", { className: "px-4 py-3 font-medium", children: a.name }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3 text-gray-600 dark:text-gray-400", children: a.description || "—" }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3", children: /* @__PURE__ */ e("span", { className: "px-2 py-0.5 text-xs rounded-full bg-gray-100 dark:bg-gray-800", children: a.userCount }) }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3 text-sm text-gray-500", children: new Date(a.createdAt).toLocaleDateString() }),
        /* @__PURE__ */ e("td", { className: "px-4 py-3", children: /* @__PURE__ */ t("div", { className: "flex gap-3", children: [
          /* @__PURE__ */ e(
            "button",
            {
              onClick: () => l.get(`/admin/roles/${a.id}/edit`),
              className: "text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm",
              children: "Edit"
            }
          ),
          /* @__PURE__ */ e(
            "button",
            {
              onClick: () => i(a.id, a.name),
              className: "text-red-600 hover:text-red-800 dark:text-red-400 text-sm",
              children: "Delete"
            }
          )
        ] }) })
      ] }, a.id)) })
    ] }) })
  ] });
}
function y() {
  function r(i) {
    i.preventDefault();
    const a = new FormData(i.currentTarget);
    l.post("/admin/roles", a);
  }
  return /* @__PURE__ */ t("div", { className: "max-w-xl mx-auto p-8", children: [
    /* @__PURE__ */ t("div", { className: "flex items-center gap-4 mb-6", children: [
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => l.get("/admin/roles"),
          className: "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300",
          children: "← Back"
        }
      ),
      /* @__PURE__ */ e("h1", { className: "text-3xl font-bold", children: "Create Role" })
    ] }),
    /* @__PURE__ */ e("form", { onSubmit: r, className: "p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: /* @__PURE__ */ t("div", { className: "space-y-4", children: [
      /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Name" }),
        /* @__PURE__ */ e(
          "input",
          {
            type: "text",
            name: "name",
            required: !0,
            className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
          }
        )
      ] }),
      /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Description" }),
        /* @__PURE__ */ e(
          "input",
          {
            type: "text",
            name: "description",
            className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
          }
        )
      ] }),
      /* @__PURE__ */ e(
        "button",
        {
          type: "submit",
          className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
          children: "Create"
        }
      )
    ] }) })
  ] });
}
function N({ role: r, users: i }) {
  function a(s) {
    s.preventDefault();
    const m = new FormData(s.currentTarget);
    l.post(`/admin/roles/${r.id}`, m);
  }
  return /* @__PURE__ */ t("div", { className: "max-w-xl mx-auto p-8", children: [
    /* @__PURE__ */ t("div", { className: "flex items-center gap-4 mb-6", children: [
      /* @__PURE__ */ e(
        "button",
        {
          onClick: () => l.get("/admin/roles"),
          className: "text-gray-500 hover:text-gray-700 dark:hover:text-gray-300",
          children: "← Back"
        }
      ),
      /* @__PURE__ */ e("h1", { className: "text-3xl font-bold", children: "Edit Role" })
    ] }),
    /* @__PURE__ */ e("form", { onSubmit: a, className: "mb-8 p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: /* @__PURE__ */ t("div", { className: "space-y-4", children: [
      /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Name" }),
        /* @__PURE__ */ e(
          "input",
          {
            type: "text",
            name: "name",
            defaultValue: r.name,
            required: !0,
            className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
          }
        )
      ] }),
      /* @__PURE__ */ t("div", { children: [
        /* @__PURE__ */ e("label", { className: "block text-sm font-medium mb-1", children: "Description" }),
        /* @__PURE__ */ e(
          "input",
          {
            type: "text",
            name: "description",
            defaultValue: r.description ?? "",
            className: "w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
          }
        )
      ] }),
      /* @__PURE__ */ t("div", { className: "text-sm text-gray-500", children: [
        "Created: ",
        new Date(r.createdAt).toLocaleString()
      ] }),
      /* @__PURE__ */ e(
        "button",
        {
          type: "submit",
          className: "px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700",
          children: "Save"
        }
      )
    ] }) }),
    /* @__PURE__ */ t("div", { className: "p-6 rounded-lg border border-gray-200 dark:border-gray-700", children: [
      /* @__PURE__ */ t("h2", { className: "text-lg font-semibold mb-4", children: [
        "Assigned Users (",
        i.length,
        ")"
      ] }),
      i.length === 0 ? /* @__PURE__ */ e("p", { className: "text-sm text-gray-500", children: "No users assigned to this role." }) : /* @__PURE__ */ e("ul", { className: "space-y-2", children: i.map((s) => /* @__PURE__ */ t("li", { className: "flex justify-between items-center py-2", children: [
        /* @__PURE__ */ t("div", { children: [
          /* @__PURE__ */ e("span", { className: "font-medium", children: s.displayName || "—" }),
          /* @__PURE__ */ e("span", { className: "text-gray-500 ml-2 text-sm", children: s.email })
        ] }),
        /* @__PURE__ */ e(
          "button",
          {
            onClick: () => l.get(`/admin/users/${s.id}/edit`),
            className: "text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm",
            children: "Edit"
          }
        )
      ] }, s.id)) })
    ] })
  ] });
}
const w = {
  "Users/Admin/Users": x,
  "Users/Admin/UsersEdit": h,
  "Users/Admin/Roles": p,
  "Users/Admin/RolesCreate": y,
  "Users/Admin/RolesEdit": N
};
export {
  w as pages
};
